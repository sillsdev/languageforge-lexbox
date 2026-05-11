import {test as base, expect} from '@playwright/test';
import {fwLiteBinaryPath, lexboxServer, projectCode} from '../config';
import {FwLiteLauncher} from './fw-lite-launcher';
import {deleteProject, logoutFromServer} from './project-operations';

const lexboxServerUrl = `${lexboxServer.protocol}://${lexboxServer.hostname}:${lexboxServer.port}`;

/**
 * `fwLite` fixture: launches FwLiteWeb pointed at the kind-cluster Lexbox,
 * navigates the page to its base URL, and tears everything down (best-effort
 * logout + project delete + process shutdown) after the test.
 *
 * Use via `test('...', async ({page, fwLite}) => { ... })`.
 */
export const test = base.extend<{fwLite: FwLiteLauncher}>({
  fwLite: async ({page}, use, testInfo) => {
    const launcher = new FwLiteLauncher();
    await launcher.launch({
      binaryPath: fwLiteBinaryPath,
      serverUrl: lexboxServerUrl,
      logFile: testInfo.outputPath('fw-lite-server.log'),
    });
    await page.goto(launcher.getBaseUrl());
    await page.waitForLoadState('networkidle');

    await use(launcher);

    try {
      // Tests may end on a project page; logout/delete operate from the home page.
      await page.goto(launcher.getBaseUrl());
      await logoutFromServer(page, lexboxServer);
      await deleteProject(page, projectCode);
    } finally {
      await launcher.shutdown();
    }
  },
});

export {expect};
