import {expect, test} from '@playwright/test';
import {FwLiteLauncher} from './helpers/fw-lite-launcher';
import {deleteProject, ensureProjectCrdtReady, logoutFromServer} from './helpers/project-operations';
import {testConfig} from './config';
import {HomePage} from './helpers/home-page';
import {ProjectPage} from './helpers/project-page';

const {lexboxServer, fwLite, testData} = testConfig;
const lexboxServerUrl = `${lexboxServer.protocol}://${lexboxServer.hostname}:${lexboxServer.port}`;
let fwLiteLauncher: FwLiteLauncher;

test.describe('FW Lite Integration Tests', () => {
  test.beforeEach(async ({page}, testInfo) => {
    fwLiteLauncher = new FwLiteLauncher();
    await fwLiteLauncher.launch({
      binaryPath: fwLite.binaryPath,
      serverUrl: lexboxServerUrl,
      timeout: fwLite.launchTimeout,
      logFile: testInfo.outputPath('fw-lite-server.log'),
    });
    await page.goto(fwLiteLauncher.getBaseUrl());
    await page.waitForLoadState('networkidle');
  });

  test.afterEach(async ({page}) => {
    try {
      // Tests may end on a project page; logout/delete operate from the home page.
      await page.goto(fwLiteLauncher.getBaseUrl());
      await logoutFromServer(page, lexboxServer);
      await deleteProject(page, testData.projectCode);
    } finally {
      await fwLiteLauncher.shutdown();
    }
  });

  test('Smoke test: application launch and server connectivity', async ({page}) => {
    const homePage = new HomePage(page);
    await homePage.waitFor();

    await homePage.ensureLoggedIn(lexboxServer, testData.testUser, testData.testPassword);
    expect(await homePage.serverProjects(lexboxServer).count()).toBeGreaterThan(0);
  });

  test('Project download: download and verify project structure', async ({page}) => {
    test.setTimeout(3 * 60_000);
    const homePage = new HomePage(page);
    await homePage.waitFor();
    await homePage.ensureLoggedIn(lexboxServer, testData.testUser, testData.testPassword);

    // sena-3 in the freshly-seeded kind cluster has no ServerCommits, so FwLite
    // filters it out of the listed projects. Trigger an initial sync server-side
    // so it shows up as a downloadable CRDT project.
    await ensureProjectCrdtReady(page, lexboxServer, testData.projectCode);

    await homePage.downloadProject(lexboxServer, testData.projectCode);
    await homePage.openLocalProject(testData.projectCode);
    await new ProjectPage(page, testData.projectCode).waitFor();
  });
});
