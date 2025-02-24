import {type APIRequestContext, type Browser, expect} from '@playwright/test';
import {test} from './fixtures';
import {LoginPage} from './pages/loginPage';
import {OidcDebuggerPage} from './pages/oidcDebuggerPage';
import {addUserToProject, loginAs, logout, preApproveOauthApp} from './utils/authHelpers';
import {OauthApprovalPage} from './pages/oauthApprovalPage';
import {elawaProjectId, serverBaseUrl} from './envVars';

// we've got a matrix of flows to test
// pre approved, not approved yet,
// logged in already, not logged in

//use https://oidcdebugger.com/ to test oauth
const flows = [
  {approved: true, loggedIn: false},
  {approved: false, loggedIn: false},
  {approved: true, loggedIn: true},
  {approved: false, loggedIn: true}
];

test.describe('oauth tests', () => {
  for (const flow of flows) {
    const name = `can login with oauth, ${flow.approved ? 'pre-approved' : 'not approved'}, ${flow.loggedIn ? 'logged in' : 'not logged in'}`;
    test(name, async ({page, tempUser}) => {

      if (flow.approved || flow.loggedIn) await loginAs(page.request, tempUser);
      if (flow.approved) {
        await preApproveOauthApp(page.request, OidcDebuggerPage.clientId, OidcDebuggerPage.scopes);
      }
      if (!flow.loggedIn) {
        await logout(page);
      }

      const oauthTestPage = await new OidcDebuggerPage(page).goto();
      await oauthTestPage.fillForm(serverBaseUrl);
      await oauthTestPage.submit();
      const approvalPage = new OauthApprovalPage(page);

      if (!flow.loggedIn) {
        await expect(page).not.toHaveURL(OidcDebuggerPage.redirectUrl);
        await approvalPage.expectNotOnPage();

        const loginPage = await new LoginPage(page).waitFor();
        await loginPage.fillForm(tempUser.email, tempUser.password);
        await loginPage.submit();
      }
      if (!flow.approved) {
        await expect(page).not.toHaveURL(OidcDebuggerPage.redirectUrl);

        await approvalPage.waitFor();
        await approvalPage.clickAuthorize();
      }

      await oauthTestPage.waitForDebuggerPage();
      const result = await oauthTestPage.getDebuggerIdToken();
      expect(result).not.toBeFalsy();
    });
  }

  async function addUserToProjectNewContext(browser: Browser, userId: string) {
    const context = await browser.newContext();
    await loginAs(context.request, 'admin');
    await addUserToProject(context.request, userId, elawaProjectId, 'EDITOR');
  }

  async function userProjectCount(apiOrToken: APIRequestContext | string) {
    if (typeof apiOrToken === 'string') {
      const response = await fetch(`${serverBaseUrl}/api/AuthTesting/token-project-count`, {method: 'GET', headers: {'authorization': `Bearer ${apiOrToken}`}});
      return parseInt(await response.text());
    } else {
      const response = await apiOrToken.get(`${serverBaseUrl}/api/AuthTesting/token-project-count`);
      return parseInt(await response.text());
    }
  }

  test('oauth generates a token from the db', async ({page, tempUserVerified, browser}) => {
    await loginAs(page.request, tempUserVerified);
    await preApproveOauthApp(page.request, OidcDebuggerPage.clientId, OidcDebuggerPage.scopes);
    await addUserToProjectNewContext(browser, tempUserVerified.id);
    const userProjectCountBefore = await userProjectCount(page.request);
    // the token hasn't been refreshed since the user was added to the project, so we shouldn't have any projects yet
    expect(userProjectCountBefore).toBe(0);
    const oauthTestPage = await new OidcDebuggerPage(page).goto();
    await oauthTestPage.fillForm(serverBaseUrl);
    await oauthTestPage.submit();
    await oauthTestPage.waitForDebuggerPage();

    const token = await oauthTestPage.getDebuggerAccessToken();
    const userProjectCountAfter = await userProjectCount(token);
    expect(userProjectCountAfter).toBe(userProjectCountBefore + 1);
  });


  test('refresh jwt just sets a header and does not return a cookie', async ({ page, tempUserVerified }) => {
    await loginAs(page.request, tempUserVerified);
    await preApproveOauthApp(page.request, OidcDebuggerPage.clientId, OidcDebuggerPage.scopes);

    const oauthTestPage = await new OidcDebuggerPage(page).goto();
    await oauthTestPage.fillForm(serverBaseUrl);
    await oauthTestPage.submit();
    await oauthTestPage.waitForDebuggerPage();

    const token = await oauthTestPage.getDebuggerAccessToken();

    const response = await fetch(`${serverBaseUrl}/api/login/refresh`, {
      method: 'POST',
      headers: {'authorization': `Bearer ${token}`}
    });
    expect(response.status).toBe(200);
    expect(response.headers.get('lexbox-jwt-updated')).toBe('all');
    expect(response.headers.get('set-cookie')).toBeNull();
  });
});
