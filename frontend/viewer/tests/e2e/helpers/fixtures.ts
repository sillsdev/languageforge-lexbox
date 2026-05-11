import {test as base, expect} from '@playwright/test';
import {fwLiteBinaryPath, lexboxServer, projectCode, serverUrl} from '../config';
import {FwLiteLauncher} from './fw-lite-launcher';
import {deleteProject} from './project-operations';
import {HomePage} from '../../pages/home.page';

/**
 * `fwLite` fixture: spawns FwLiteWeb pointed at the kind-cluster Lexbox,
 * navigates to its base URL, and tears down (best-effort logout + project
 * delete + process shutdown) after the test.
 */
export const test = base.extend<{fwLite: FwLiteLauncher}>({
  fwLite: async ({page}, use, testInfo) => {
    const launcher = new FwLiteLauncher();
    await launcher.launch({
      binaryPath: fwLiteBinaryPath,
      serverUrl: serverUrl(lexboxServer),
      logFile: testInfo.outputPath('fw-lite-server.log'),
    });
    await page.goto(launcher.getBaseUrl());
    await page.waitForLoadState('networkidle');

    await use(launcher);

    try {
      // Tests may end on a project page; logout/delete operate from the home page.
      await page.goto(launcher.getBaseUrl());
      await new HomePage(page).ensureLoggedOut(lexboxServer);
      await deleteProject(page, projectCode);
    } finally {
      await launcher.shutdown();
    }
  },
});

export {expect};
