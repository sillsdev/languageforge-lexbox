import { expect, test } from '@playwright/test';
import { SandboxPage } from './pages/sandboxPage';
import * as testEnv from './envVars';
import { UserDashboardPage } from './pages/userDashboardPage';
import { LoginPage } from './pages/loginPage';
import { AdminDashboardPage } from './pages/adminDashboardPage';
import { loginAs } from './authHelpers';

test('can catch 500 errors from goto in same tab', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/testing/test500NoException');
  await page.getByText('Goto API 500', {exact: true}).click();
  const response = await responsePromise;
  expect(response.status()).toBe(500);
});

test('can catch 500 errors from goto in new tab', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const responsePromise = context.waitForEvent('response', response => response.url().endsWith('/api/testing/test500NoException'));
  await page.getByText('Goto API 500 new tab').click();
  const response = await responsePromise;
  expect(response.status()).toBe(500);
});

test('can catch 500 errors in page load', async ({ page }) => {
  await new SandboxPage(page).goto();
  await page.getByText('Goto page load 500', {exact: true}).click();
  // eslint-disable-next-line @typescript-eslint/quotes
  await expect(page.locator(":text-matches('Unexpected response:.*(500)', 'g')").first()).toBeVisible();
});

test('page load 500 lands on new page', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const pagePromise = context.waitForEvent('page', page => page.url().endsWith('/sandbox/500'));
  await page.getByText('Goto page load 500 new tab').click();
  const newPage = await pagePromise;
  // eslint-disable-next-line @typescript-eslint/quotes
  await expect(newPage.locator(":text-matches('Unexpected response:.*(500)', 'g')").first()).toBeVisible();
});

// Locator is wrong, investigate and fix
test.skip('catch fetch 500 and error dialog', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/testing/test500NoException');
  await page.getByText('Goto API 500', {exact: true}).click();
  await responsePromise;
  // eslint-disable-next-line @typescript-eslint/quotes
  await expect(page.locator(":text-matches('Unexpected response:.*(500)', 'g')").first()).toBeVisible();
});

test('server page load 403 is redirected to login', async ({ context }) => {
  await context.addCookies([{name: testEnv.authCookieName, value: testEnv.invalidJwt, url: testEnv.serverBaseUrl}]);
  const page = await context.newPage();
  await new UserDashboardPage(page).goto({expectRedirect: true});
  await new LoginPage(page).waitFor();
});

// Getting 401, not 403. Need to investigate
test.skip('client page load 403 is redirected to login', async ({ request, browser }) => {
  // TODO: Move this to a setup script as recommended by https://playwright.dev/docs/auth
  await loginAs(request, 'admin', testEnv.defaultPassword);
  const adminContext = await browser.newContext({storageState: 'admin-storageState.json'});
  const adminPage = await adminContext.newPage();
  const adminDashboardPage = await new AdminDashboardPage(adminPage).goto();

  // Now mess up the login cookie and watch the redirect

  await adminContext.addCookies([{name: testEnv.authCookieName, value: testEnv.invalidJwt, url: testEnv.serverBaseUrl, httpOnly: true}]);
  const responsePromise = adminPage.waitForResponse('/api/graphql');
  await adminDashboardPage.clickProject('Sena 3');
  const graphqlResponse = await responsePromise;
  expect(graphqlResponse.status()).toBe(403);
  await new LoginPage(adminPage).waitFor();
});

test('can catch 403 errors from goto in same tab', async ({ page }) => {
  await new SandboxPage(page).goto();
  // Create promise first before triggering the action
  const responsePromise = page.waitForResponse('/api/AuthTesting/403');
  await page.getByText('Goto API 403', {exact: true}).click();
  const response = await responsePromise;
  expect(response.status()).toBe(403);
});

test('can catch 403 errors from goto in new tab', async ({ page, context }) => {
  await new SandboxPage(page).goto();
  const responsePromise = context.waitForEvent('response', response => response.url().endsWith('/api/AuthTesting/403'));
  await page.getByText('Goto API 403 new tab').click();
  const response = await responsePromise;
  expect(response.status()).toBe(403);
});

test('page load 403 is redirected to home', async ({ request, browser }) => {
  await loginAs(request, 'manager', testEnv.defaultPassword);
  const managerContext = await browser.newContext({storageState: 'manager-storageState.json'});
  const managerPage = await managerContext.newPage();
  await new SandboxPage(managerPage).goto();
  await managerPage.getByText('Goto page load 403', {exact: true}).click();
  // eslint-disable-next-line @typescript-eslint/quotes
  await new UserDashboardPage(managerPage).waitFor();
});

test('page load 403 in new tab is redirected to home', async ({ request, browser }) => {
  await loginAs(request, 'manager', testEnv.defaultPassword);
  const managerContext = await browser.newContext({storageState: 'manager-storageState.json'});
  const managerPage = await managerContext.newPage();
  await new SandboxPage(managerPage).goto();
  const pagePromise = managerContext.waitForEvent('page');
  await managerPage.getByText('Goto page load 403 new tab').click();
  const newPage = await pagePromise;
  // eslint-disable-next-line @typescript-eslint/quotes
  await new UserDashboardPage(newPage).waitFor();
});
