import {AdminDashboardPage} from './pages/adminDashboardPage';
import {loginAs} from './utils/authHelpers';
import {test} from './fixtures';

test('Back button after logout redirects back to login page', async ({page}) => {
  await loginAs(page.request, 'admin');
  const adminPage = await new AdminDashboardPage(page).goto();
  const drawer = await adminPage.openDrawer();
  const loginPage = await drawer.logout();
  await page.goBack();
  await loginPage.waitFor();
});
