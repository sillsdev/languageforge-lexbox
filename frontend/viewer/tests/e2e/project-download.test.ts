import {test} from './helpers/fixtures';
import {ensureProjectCrdtReady} from './helpers/project-operations';
import {lexboxServer, projectCode, testPassword, testUser} from './config';
import {HomePage} from '../pages/home.page';
import {ProjectPage} from '../pages/project.page';

test('Project download: log in, download, and open the project', async ({page, fwLite: _fwLite}) => {
  test.setTimeout(3 * 60_000);

  const homePage = new HomePage(page);
  await homePage.waitFor();
  await homePage.ensureLoggedIn(lexboxServer, testUser, testPassword);

  // sena-3 in the freshly-seeded kind cluster has no ServerCommits, so FwLite
  // filters it out of the listed projects. Trigger an initial sync server-side
  // so it shows up as a downloadable CRDT project.
  await ensureProjectCrdtReady(page, lexboxServer, projectCode);

  await homePage.downloadProject(lexboxServer, projectCode);
  await homePage.openLocalProject(projectCode);
  await new ProjectPage(page).waitFor();
});
