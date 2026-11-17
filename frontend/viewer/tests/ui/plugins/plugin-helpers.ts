import {readFile} from 'node:fs/promises';
import http from 'node:http';
import type {AddressInfo} from 'node:net';
import {expect, type Download, type Page} from '@playwright/test';
import {DemoProjectPage} from '../demo-project.page';
import {STORAGE_KEY as RELEASE_CHANNEL_KEY} from '../../../src/lib/feature-flags/feature-flags';

/** Plugins are a beta-channel feature: call this from a `test.beforeEach`. */
export async function enablePlugins(page: Page) {
  await page.addInitScript((key) => localStorage.setItem(key, 'beta'), RELEASE_CHANNEL_KEY);
}

export async function gotoPlugins(page: Page) {
  await new DemoProjectPage(page).goto();
  await page.getByRole('button', {name: 'Plugins'}).click();
  await expect(page.getByRole('heading', {name: 'Plugins', exact: true})).toBeVisible();
}

export function pluginHtml(title: string, options: {head?: string; body: string}): string {
  return [
    `<!DOCTYPE html><html><head><title>${title}</title>${options.head ?? ''}</head><body>`,
    options.body,
    '</body></html>',
  ].join('\n');
}

export async function readDownload(download: Download): Promise<string> {
  return readFile(await download.path(), 'utf8');
}

/** Pastes HTML into the open studio; the reply may carry chat prose around the document. */
export async function pasteIntoStudio(page: Page, html: string) {
  await page.getByLabel(/Paste (the AI's reply|HTML|a new version)/).fill(`Here is your plugin:\n\n${html}\n\nEnjoy!`);
  await page.getByRole('button', {name: 'Use this version'}).click();
}

/** Straight to the studio by URL: skips rendering the project view and the Plugins page. */
export async function openNewPluginStudio(page: Page) {
  await page.goto('/testing/project-view/plugins/new');
  await expect(page.getByTestId('plugin-studio')).toBeVisible();
}

/** Opens a new plugin in the studio from pasted HTML. An offline plugin starts in the preview at once. */
export async function previewInStudio(page: Page, html: string) {
  await openNewPluginStudio(page);
  await pasteIntoStudio(page, html);
}

/** Builds a plugin in the studio from pasted HTML and adds it, returning to the Plugins page. */
export async function addPluginViaStudio(page: Page, html: string) {
  await previewInStudio(page, html);
  await page.getByRole('button', {name: 'Add to project'}).click();
  await expect(page.getByText('Added for the team')).toBeVisible();
  await page.getByRole('button', {name: 'Back to plugins'}).click();
}

export function projectCard(page: Page, name: string) {
  return page.getByTestId('project-plugin-card').filter({hasText: name});
}

export interface RequestSink {
  url: string;
  port: number;
  /** Every request that reached the server, as `METHOD /path`. */
  requests: string[];
  close(): Promise<void>;
}

/** A local HTTP server standing in for "the internet": whatever a plugin gets out lands in `requests`. */
export async function startRequestSink(): Promise<RequestSink> {
  const requests: string[] = [];
  const server = http.createServer((req, res) => {
    requests.push(`${req.method} ${req.url}`);
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.end('ok');
  });
  server.on('upgrade', (req, socket) => {
    requests.push(`upgrade ${req.url}`);
    socket.destroy();
  });
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const {port} = server.address() as AddressInfo;
  return {
    url: `http://127.0.0.1:${port}`,
    port,
    requests,
    close: () => new Promise((resolve) => server.close(() => resolve())),
  };
}
