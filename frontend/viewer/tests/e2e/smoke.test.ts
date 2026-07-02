import {expect, test} from './helpers/fixtures';
import {lexboxServer, testPassword, testUser} from './config';
import {HomePage} from '../pages/home.page';

test('FW Lite launches and connects to the server', async ({page, fwLite: _fwLite}) => {
  const homePage = new HomePage(page);
  await homePage.waitFor();

  await homePage.ensureLoggedIn(lexboxServer, testUser, testPassword);
  // Server projects load asynchronously after login; poll rather than snapshot the count once.
  await expect.poll(() => homePage.serverProjects(lexboxServer).count()).toBeGreaterThan(0);
});
