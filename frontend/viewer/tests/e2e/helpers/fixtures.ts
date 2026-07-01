import {test as base, expect} from '@playwright/test';
import {fwLiteBinaryPath, lexboxServer, projectCode, serverUrl} from '../config';
import {FwLiteLauncher} from './fw-lite-launcher';
import {deleteProject} from './project-operations';
import {HomePage} from '../../pages/home.page';

// Teardown is best-effort: a test that failed mid-flow may leave nothing to log out or delete.
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
