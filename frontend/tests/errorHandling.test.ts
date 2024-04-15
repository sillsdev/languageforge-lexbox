import * as testEnv from './envVars';

import { AdminDashboardPage } from './pages/adminDashboardPage';
import { LoginPage } from './pages/loginPage';
import { SandboxPage } from './pages/sandboxPage';
import { UserAccountSettingsPage } from './pages/userAccountSettingsPage';
import { UserDashboardPage } from './pages/userDashboardPage';
import { expect } from '@playwright/test';
import { getInbox } from './utils/mailboxHelpers';
import { loginAs } from './utils/authHelpers';
import { test } from './fixtures';

test('can catch 500 errors from goto in same tab', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/testing/test500NoException');
  await page.getByText('Goto API 500', {exact: true}).click();
  const response = await responsePromise;
  expect(response.status()).toBe(500);
  test.fail();
});

test('can catch 500 errors from goto in new tab', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const responsePromise = context.waitForEvent('response', response => response.url().endsWith('/api/testing/test500NoException'));
  await page.getByText('Goto API 500 new tab').click();
  const response = await responsePromise;
  expect(response.status()).toBe(500);
  test.fail();
});

test('can catch 500 errors in page load', async ({ page }) => {
  await new SandboxPage(page).goto();
  await page.getByText('Goto page load 500', {exact: true}).click();
  await expect(page.locator(':text-matches("Unexpected response:.*(500)", "g")').first()).toBeVisible();
  test.fail();
});

test('page load 500 lands on new page', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const pagePromise = context.waitForEvent('page', page => page.url().endsWith('/sandbox/500'));
  await page.getByText('Goto page load 500 new tab').click();
  const newPage = await pagePromise;
  await expect(newPage.locator(':text-matches("Unexpected response:.*(500)", "g")').first()).toBeVisible();
  test.fail();
});

// Locator is wrong, investigate and fix
test('catch fetch 500 and error dialog', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/testing/test500NoException');
  await page.getByText('Fetch 500').click();
  await responsePromise;
  await expect(page.locator(':text-matches("Unexpected response:.*(500)", "g")').first()).toBeVisible();
  test.fail();
});

//we want to verify that once we get the 500 in GQL we can still navigate to another page
test('client-side gql 500 does not break the application', async ({ page }) => {
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/graphql');
  await page.getByText('GQL 500').click();
  await responsePromise.catch(() => { });// Ignore the error
  await expect(page.locator(':text-matches("Unexpected response:.*(500)", "g")').first()).toBeVisible();
  await page.getByRole('button', { name: 'Dismiss' }).click();
  await page.getByText('Language Depot').click();
  await new UserDashboardPage(page).waitFor();
  test.fail(); // Everything up to here passed, but we expect a soft 500 response assertion to ultimately fail the test
});

test('server-side gql 500 does not kill the server', async ({ page }) => {
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  await new SandboxPage(page).goto({ urlEnd: '?ssr-gql-500', expectErrorResponse: true });
  await expect(page.locator(':text-matches("Unexpected response:.*(500)", "g")').first()).toBeVisible();
  // we've verified that a 500 occured, now we verify that the server is still alive
  await new AdminDashboardPage(page).goto();
  test.fail(); // Everything up to here passed, but we expect a soft 500 response assertion to ultimately fail the test
});

test('server page load 401 is redirected to login', async ({ context }) => {
  await context.addCookies([{name: testEnv.authCookieName, value: testEnv.invalidJwt, url: testEnv.serverBaseUrl}]);
  const page = await context.newPage();
  await new UserDashboardPage(page).goto({expectRedirect: true});
  await new LoginPage(page).waitFor();
});

test('client page load 401 is redirected to login', async ({ page }) => {
  // TODO: Move this to a setup script as recommended by https://playwright.dev/docs/auth
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  const adminDashboardPage = await new AdminDashboardPage(page).goto();

  // Now mess up the login cookie and watch the redirect

  await page.context().addCookies([{name: testEnv.authCookieName, value: testEnv.invalidJwt, url: testEnv.serverBaseUrl}]);
  const responsePromise = page.waitForResponse('/api/graphql');
  await adminDashboardPage.clickProject('Sena 3');
  const graphqlResponse = await responsePromise;
  expect(graphqlResponse.status()).toBe(401);
  await new LoginPage(page).waitFor();
});

test('can catch 403 errors from goto in same tab', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/AuthTesting/403');
  await page.getByText('Goto API 403', {exact: true}).click();
  const response = await responsePromise;
  expect(response.status()).toBe(403);
  test.fail();
});

test('can catch 403 errors from goto in new tab', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const responsePromise = context.waitForEvent('response', response => response.url().endsWith('/api/AuthTesting/403'));
  await page.getByText('Goto API 403 new tab').click();
  const response = await responsePromise;
  expect(response.status()).toBe(403);
  test.fail();
});

test('page load 403 is redirected to home', async ({ page }) => {
  await loginAs(page.request, 'manager', testEnv.defaultPassword);
  await new SandboxPage(page).goto();
  await page.getByText('Goto page load 403', {exact: true}).click();
  await new UserDashboardPage(page).waitFor();
});

test('page load 403 in new tab is redirected to home', async ({ page }) => {
  await loginAs(page.request, 'manager', testEnv.defaultPassword);
  await new SandboxPage(page).goto();
  const pagePromise = page.context().waitForEvent('page');
  await page.getByText('Goto page load 403 new tab').click();
  const newPage = await pagePromise;
  await new UserDashboardPage(newPage).waitFor();
});

test('page load 403 on home page is redirected to login', async ({ page, tempUser }) => {
  test.slow();
  // (1) Get JWT with only forgot-password audience

  // - Request forgot password email
  await page.goto('/logout');
  const loginPage = await new LoginPage(page).goto();
  const forgotPasswordPage = await loginPage.clickForgotPassword();
  await forgotPasswordPage.fillForm(tempUser.email);
  await forgotPasswordPage.submit();

  // - Get JWT from reset password link
  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  const emailPage = await inboxPage.openEmail();
  const url = await emailPage.getFirstLanguageDepotUrl();
  expect(url).not.toBeNull();
  const forgotPasswordJwt = (url as string).split('jwt=')[1].split('&')[0];

  // (2) Get to a non-home page with an empty urql cache
  await loginAs(page.request, tempUser.email, tempUser.password);
  const userAccountPage = await new UserAccountSettingsPage(page).goto();

  // (3) Update cookie with the reset-password JWT and try to go home
  await page.context().addCookies([{name: testEnv.authCookieName, value: forgotPasswordJwt, url: testEnv.serverBaseUrl}]);

  const responsePromise = page.waitForResponse('/api/graphql');
  await userAccountPage.clickHome();
  const response = await responsePromise;
  expect(response.status()).toBe(403);

  // (4) Expect to be redirected to login page
  await new LoginPage(page).waitFor();
});

test('node survives corrupt jwt', async ({ page }) => {
  const corruptJwt = 'bla-bla-bla';
  await page.context().addCookies([{name: testEnv.authCookieName, value: corruptJwt, url: testEnv.serverBaseUrl}]);
  await new UserDashboardPage(page).goto({expectRedirect: true});
  await new LoginPage(page).waitFor();
});
