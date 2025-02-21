import {expect} from '@playwright/test';
import {test} from './fixtures';
import {LoginPage} from './pages/loginPage';
import {defaultPassword} from './envVars';
import {OidcDebuggerPage} from './pages/oidcDebuggerPage';
import {loginAs, logout} from './utils/authHelpers';
import {OauthApprovalPage} from './pages/oauthApprovalPage';

// we've got a matrix of flows to test
// pre approved, not approved yet,
// logged in already, not logged in

//use https://oidcdebugger.com/ to test oauth
const flows = [{approved: true, loggedIn: false}, {approved: false, loggedIn: false}, {approved: true, loggedIn: true}, {approved: false, loggedIn: true}];
test.describe('oauth tests', () => {
  for (const flow of flows) {
    const name = `can login with oauth, ${flow.approved ? 'pre-approved' : 'not approved'}, ${flow.loggedIn ? 'logged in' : 'not logged in'}`;
    test(name, async ({page, baseURL, tempUser}) => {
      if (!baseURL) throw new Error('baseURL is not set');
      if (flow.loggedIn) {
        await loginAs(page.request, tempUser);
      } else {
        await logout(page);
      }

      const oauthTestPage = await new OidcDebuggerPage(page).goto();
      await oauthTestPage.fillForm(baseURL);
      await oauthTestPage.submit();
      const approvalPage = new OauthApprovalPage(page);

      if (!flow.loggedIn) {
        await expect(page).not.toHaveURL(OidcDebuggerPage.redirectUrl);
        await approvalPage.expectNotOnPage();

        const loginPage = await new LoginPage(page).waitFor();
        await loginPage.fillForm('admin', defaultPassword);
        await loginPage.submit();
      }
      if (!flow.approved) {
        await expect(page).not.toHaveURL(OidcDebuggerPage.redirectUrl);

        await approvalPage.waitFor();
        await approvalPage.clickAuthorize();
      }

      await oauthTestPage.waitForDebuggerPage();
      const result = await oauthTestPage.getDebuggerIdToken();
      console.log('result', result);
      expect(result).toContain('id_token=');
    });
  }
});
