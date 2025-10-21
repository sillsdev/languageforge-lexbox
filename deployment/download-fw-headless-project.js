#!/usr/bin/env node

import {execSync, spawn} from "child_process";

import fs from "fs";
import http from "http";
import path from "path";
import {pipeline} from "stream";
import {promisify} from "util";
import zlib from "zlib";

const pipe = promisify(pipeline);

// ============================
// Parse command-line arguments
// ============================
const args = process.argv.slice(2);
if (args.length < 4) {
  console.error("Usage: node downloadProject.js <projectId> <projectCode> <context> <namespace>");
  process.exit(1);
}
const [projectId, projectCode, context, namespace] = args;

// ============================
// Variables
// ============================
const projectFolder = `/var/lib/fw-headless/projects/${projectCode}-${projectId}`;
const tempDir = "/tmp";
const tarFile = `${projectCode}-project.tar.gz`;
const remoteTar = `${tempDir}/${tarFile}`;
const localTar = path.resolve(tarFile);
const timestamp = new Date().toISOString().replace(/[-:T]/g, "").split(".")[0]; // e.g. 20251020_143522
const localExtractDir = path.resolve(`_downloads/${projectCode}-${timestamp}`);
const podLabel = "app=fw-headless";
const portLocal = 8088;
const portPod = 8087;
const tarDownloadUrl = `http://localhost:${portLocal}/${tarFile}`;

let pod;
let pfProcess;

// ============================
// Helper to run kubectl commands
// ============================
function runKubectl(args, options = {}) {
  return execSync(`kubectl ${args.join(" ")}`, { stdio: "pipe", ...options }).toString().trim();
}

// ============================
// 1️⃣ Find the pod
// ============================
function findPod() {
  pod = runKubectl([
    "--context", context,
    "get", "pod",
    "-n", namespace,
    "-l", podLabel,
    "-o", "jsonpath={.items[0].metadata.name}"
  ]);
  console.log(`Using pod: ${pod}`);
}

// ============================
// 2️⃣ Tar the project folder in the pod
// ============================
function tarProjectInPod() {
  console.log("Tarring project folder in pod...");
  runKubectl([
    "exec", "--context", context, "-n", namespace, "-c", "fw-headless", pod, "--",
    "sh", "-c",
    `"cd ${path.dirname(projectFolder)} && tar czf ${remoteTar} $(basename ${projectFolder})"`
  ]);
}

// ============================
// 3️⃣ Start Python HTTP server in pod
// ============================
function startPythonServer() {
  console.log("Starting Python HTTP server in pod...");

  const cmd = `kubectl exec --context ${context} -n ${namespace} -c fw-headless ${pod} -- sh -c "cd ${tempDir} && nohup python3 -m http.server ${portPod} > /dev/null 2>&1 & echo $! > ${tempDir}/python.pid"`;

  execSync(cmd, { stdio: "inherit" });
  console.log("Python HTTP server started in background");
}

// ============================
// 4️⃣ Port-forward to local machine
// ============================
function startPortForward() {
  console.log("Starting port-forward...");
  pfProcess = spawn("kubectl", [
    "--context", context,
    "port-forward", "-n", namespace,
    `pod/${pod}`,
    `${portLocal}:${portPod}`
  ], { stdio: "inherit" });
}

// ============================
// 5️⃣ Wait for server
// ============================
async function waitForServerReady() {
  console.log("Waiting for Python HTTP server to be ready...");
  const maxRetries = 20;
  let retry = 0;
  while (retry < maxRetries) {
    try {
      const res = await fetch(`http://localhost:${portLocal}`, { method: "HEAD" });
      if (res.ok || res.status === 403) break;
    } catch {}
    await new Promise(r => setTimeout(r, 1000));
    retry++;
  }
  if (retry >= maxRetries) throw new Error("Python HTTP server did not start in time.");
}

// ============================
// 6️⃣ Download tar.gz and extract
// ============================
async function downloadAndExtract() {
  console.log("Downloading tar.gz and extracting...");
  fs.mkdirSync(localExtractDir, { recursive: true });

  await new Promise((resolve, reject) => {
    http.get(tarDownloadUrl, (res) => {
      if (res.statusCode !== 200)
        return reject(new Error(`Failed to download: ${res.statusCode}`));

      const total = parseInt(res.headers["content-length"] || "0", 10);
      let downloaded = 0;
      const chunks = [];

      console.log(`Starting download, total size: ${total} bytes`);
      let lastLoggedPercent = 0;

      res.on("data", (chunk) => {
        chunks.push(chunk);
        downloaded += chunk.length;

        // Log progress every 5%
        if (total) {
          const pct = Math.floor((downloaded / total) * 100);
          if (pct >= lastLoggedPercent + 5) {
            console.log(`Downloading... ${pct}%`);
            lastLoggedPercent = pct;
          }
        } else {
          // For unknown size, log every 10MB
          const mb = Math.floor(downloaded / (10 * 1024 * 1024));
          if (mb > lastLoggedPercent) {
            console.log(`Downloading... ${Math.round(downloaded / (1024 * 1024))} MB`);
            lastLoggedPercent = mb;
          }
        }
      });

      res.on("end", () => {
        process.stdout.write("\nExtracting...\n");
        const buffer = Buffer.concat(chunks);
        const decompressed = zlib.gunzipSync(buffer);
        extractTar(decompressed, localExtractDir);
        resolve();
      });

      res.on("error", reject);
    });
  });

  // delete @LongLink tar header
  const longLink = path.join(localExtractDir, "@LongLink");
  if (fs.existsSync(longLink)) fs.unlinkSync(longLink);

  console.log(`Project extracted to ${localExtractDir}`);
}

// Minimal tar extraction helper (supports regular files, ignores symlinks)
function extractTar(buffer, extractDir) {
  let offset = 0;

  while (offset < buffer.length) {
    const header = buffer.slice(offset, offset + 512);
    const name = header.toString("utf8", 0, 100).replace(/\0.*$/, "");
    if (!name) break; // end of tar

    const size = parseInt(header.toString("utf8", 124, 136).replace(/\0.*$/, ""), 8);
    const totalSize = 512 + Math.ceil(size / 512) * 512;

    const fileContent = buffer.slice(offset + 512, offset + 512 + size);
    const filePath = path.join(extractDir, name);
    const dir = path.dirname(filePath);
    ensureDir(dir);
    fs.writeFileSync(filePath, fileContent);

    offset += totalSize;
  }
}

function ensureDir(dirPath) {
  if (fs.existsSync(dirPath)) {
    if (!fs.lstatSync(dirPath).isDirectory()) {
      // If a file exists with the same name, remove it
      fs.unlinkSync(dirPath);
      fs.mkdirSync(dirPath, { recursive: true });
    }
    // already a directory — do nothing
  } else {
    fs.mkdirSync(dirPath, { recursive: true });
  }
}

// ============================
// 7️⃣ Cleanup pod
// ============================
function cleanupPod() {
  console.log("Cleaning up pod...");
  runKubectl([
    "exec", "--context", context, "-n", namespace, "-c", "fw-headless", pod, "--",
    "sh", "-c",
    `kill $(cat ${tempDir}/python.pid) 2>/dev/null || true && rm -f ${tempDir}/python.pid ${remoteTar}`
  ]);
  if (pfProcess && !pfProcess.killed) pfProcess.kill();
}

// ============================
// Main sequence
// ============================
(async () => {
  try {
    findPod();
    tarProjectInPod();
    startPythonServer();
    startPortForward();
    await waitForServerReady();
    await downloadAndExtract();
    cleanupPod();
    console.log("✅ Done!");
  } catch (err) {
    console.error("❌ Error:", err);
    if (pfProcess && !pfProcess.killed) pfProcess.kill();
  }
})();
