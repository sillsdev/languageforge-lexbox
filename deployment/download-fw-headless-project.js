#!/usr/bin/env node

import {execFileSync, execSync} from "child_process";

import fs from "fs";
import path from "path";

// ============================
// Parse command-line arguments
// ============================
const args = process.argv.slice(2);
if (args.length < 4) {
  console.error("Usage: node download-fw-headless-project.js <projectId> <projectCode> <context> <namespace>");
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
const localTar = `_downloads/_tars/${tarFile}`;
const timestamp = new Date().toISOString().replace(/[-:T]/g, "").split(".")[0]; // e.g. 20251020143522
const localExtractDir = path.resolve(`_downloads/${projectCode}-${projectId}_${timestamp}`);
const podLabel = "app=fw-headless";

// ============================
// Helper to run kubectl commands
// ============================
function runKubectl(args, options = {}) {
  // Set environment variable to make kubectl cp reliable for large downloads
  const env = { ...process.env, KUBECTL_REMOTE_COMMAND_WEBSOCKETS: "false" };
  return execSync(`kubectl ${args.join(" ")}`, { stdio: "pipe", env, ...options })?.toString().trim();
}

// ============================
// 1️⃣ Find the pod
// ============================
function findPod() {
  const pod = runKubectl([
    "--context", context,
    "get", "pod",
    "-n", namespace,
    "-l", podLabel,
    "-o", "jsonpath={.items[0].metadata.name}"
  ]);
  console.log(`Using pod: ${pod}`);
  return pod;
}

// ============================
// 2️⃣ Tar the project folder in the pod
// ============================
function tarProjectInPod(pod) {
  console.log("Tarring project folder in pod...");
  runKubectl([
    "exec", "--context", context, "-n", namespace, "-c", "fw-headless", pod, "--",
    "sh", "-c",
    `"cd ${projectFolder} && tar czf ${remoteTar} ."`
  ]);
}

// ============================
// 3️⃣ Copy tar file from pod to local machine
// ============================
function copyTarFromPod(pod) {
  console.log("Copying tar file from pod...");
  runKubectl([
    "cp", "--retries", 100, "--context", context, "-n", namespace,
    "-c", "fw-headless",
    `${pod}:${remoteTar}`,
    localTar
  ], { stdio: "inherit" });
  console.log(`Tar file copied to ${localTar}`);
}

// ============================
// 4️⃣ Extract tar file locally
// ============================
function extractTarFile() {
  console.log("Extracting tar file...");
  fs.mkdirSync(localExtractDir, { recursive: true });

  // Relative posix path: GNU tar (e.g. Git Bash on PATH) reads the colon in "D:\..." as host:path and rejects backslashes
  const extractDir = path.relative(process.cwd(), localExtractDir).split(path.sep).join("/");
  execFileSync("tar", ["-xzf", localTar, "-C", extractDir], { stdio: "inherit" });

  console.log(`Project extracted to ${localExtractDir}`);
}

// ============================
// 5️⃣ Cleanup pod and local files
// ============================
function cleanup(pod, keepLocalTar) {
  console.log("Cleaning up...");

  // Remove tar file from pod
  try {
    runKubectl([
      "exec", "--context", context, "-n", namespace, "-c", "fw-headless", pod, "--",
      "rm", "-f", remoteTar
    ]);
  } catch (error) {
    console.error("Failed to remove tar file from pod:", error.message);
  }

  // Remove local tar file
  try {
    if (!keepLocalTar && fs.existsSync(localTar)) {
      fs.unlinkSync(localTar);
    } else if (keepLocalTar) {
      console.log(`Kept ${localTar} so the extract can be retried without another download`);
    }
  } catch (error) {
    console.error("Failed to remove local tar file:", error.message);
  }
}

// ============================
// Main sequence
// ============================
function main() {
  const pod = findPod();
  if (!pod) throw new Error("No pod found.");

  let extracted = false;
  try {
    tarProjectInPod(pod);
    copyTarFromPod(pod);
    extractTarFile();
    extracted = true;
    console.log("✅ Done!");
  } catch (err) {
    console.error("❌ Error:", err);
  } finally {
    cleanup(pod, !extracted && fs.existsSync(localTar));
  }
}

// 🚀 Off we go!
main();
