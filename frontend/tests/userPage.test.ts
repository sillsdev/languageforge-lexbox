import { expect } from '@playwright/test';
import { test } from './fixtures';
import { UserDashboardPage } from './pages/userDashboardPage';
import { UserAccountSettingsPage } from './pages/userAccountSettingsPage';
import { randomUUID } from 'crypto';
import { loginAs, logout } from './authHelpers';
import { getInbox } from './mailboxHelpers';

test('can update account info', async ({ page, tempUser }) => {
  await loginAs(page.request, tempUser.name, tempUser.password);
  const userPage = await new UserAccountSettingsPage(page).goto();
  const newUuid = randomUUID();
  await userPage.fillDisplayName('Test: Edit account - update - changed');
  await userPage.fillEmail(`${newUuid}_changed@test.com`);
  await userPage.clickSave();
  await page.getByText('Your account has been updated.').waitFor();
  await userPage.emailVerificationAlert.assertRequestedToChange();
});

test('display form errors on invalid data', async ({ page, tempUser }) => {
  await loginAs(page.request, tempUser.name, tempUser.password);
  const userPage = await new UserAccountSettingsPage(page).goto();
  await userPage.fillDisplayName('');
  await userPage.fillEmail('');
  await userPage.clickSave();
  await expect(page.getByText('Invalid email')).toBeVisible();
});

test('can reset password', async ({ page, tempUser }) => {
  const newPassword = 'test_edit_account_reset_password_changed';
  expect(tempUser.password).not.toBe(newPassword);
  await loginAs(page.request, tempUser.name, tempUser.password);
  const userPage = await new UserAccountSettingsPage(page).goto();
  const resetPasswordPage = await userPage.clickResetPassword();
  await resetPasswordPage.fillForm(newPassword);
  await resetPasswordPage.submit();

  const loginPage = await logout(page);
  await loginPage.fillForm(tempUser.email, newPassword);
  await loginPage.submit();

  await new UserDashboardPage(loginPage.page).waitFor();

  // Verify password changed email was received
  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  const emailPage = await inboxPage.openEmail();
  await expect(emailPage.page.getByText('Your password was changed').first()).toBeVisible();
});
