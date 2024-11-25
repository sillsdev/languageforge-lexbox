import {AdminDashboardPage} from './pages/adminDashboardPage';
import {loginAs} from './utils/authHelpers';
import {test} from './fixtures';

test('Back button after logout redirects back to login page', async ({page, browserName}) => {
  test.skip(browserName === 'firefox', 'Support for Clear-Site-Data: "cache" was removed and is WIP (https://bugzilla.mozilla.org/show_bug.cgi?id=1838506)');
  await loginAs(page.request, 'admin');
  const adminPage = await new AdminDashboardPage(page).goto();
  const drawer = await adminPage.openDrawer();
  const loginPage = await drawer.logout();
  await page.goBack();
  await loginPage.waitFor();
});
