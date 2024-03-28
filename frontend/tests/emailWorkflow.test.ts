import { expect } from '@playwright/test';
import { test } from './fixtures';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';
import { UserDashboardPage } from './pages/userDashboardPage';
import { loginAs, logout } from './utils/authHelpers';
import { getInbox } from './utils/mailboxHelpers';
import { UserAccountSettingsPage } from './pages/userAccountSettingsPage';
import { ResetPasswordPage } from './pages/resetPasswordPage';
import { randomUUID } from 'crypto';
import { LoginPage } from './pages/loginPage';

test('register, verify, update, verify email address', async ({ page, tempUser }) => {
  test.slow(); // Checking email and logging in repeatedly takes time
  await loginAs(page.request, tempUser.email, tempUser.password);
  const userDashboardPage = await new UserDashboardPage(page).goto();
  await userDashboardPage.emailVerificationAlert.assertPleaseVerify();

  // Request extra verification email and verify
  await userDashboardPage.emailVerificationAlert.clickResendEmail();
  await userDashboardPage.emailVerificationAlert.assertVerificationSent();

  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  await expect(inboxPage.emailLocator).toHaveCount(2);
  let emailPage = await inboxPage.openEmail();
  let pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickVerifyEmail();
  let newPage = await pagePromise;
  let userPage = await new UserAccountSettingsPage(newPage).waitFor();

  await userPage.emailVerificationAlert.assertSuccessfullyVerified();

  // Verify verification alert goes away on navigation
  await userPage.clickHome();
  await new UserDashboardPage(userPage.page).waitFor();
  await userPage.emailVerificationAlert.assertGone();

  // Request new email address
  const newMailinatorId = randomUUID();
  const newEmail = `${newMailinatorId}@mailinator.com`;
  await userPage.goto();
  await userPage.fillEmail(newEmail);
  await userPage.clickSave();

  await userPage.emailVerificationAlert.assertRequestedToChange();

  // Verify new email address
  await inboxPage.gotoMailbox(newMailinatorId);
  await expect(inboxPage.emailLocator).toHaveCount(1);
  emailPage = await inboxPage.openEmail();
  pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickVerifyEmail();
  newPage = await pagePromise;
  userPage = await new UserAccountSettingsPage(newPage).waitFor();

  await userPage.emailVerificationAlert.assertSuccessfullyUpdated();

  // Confirm new meail address works
  const loginPage = await logout(page);
  await loginPage.fillForm(newEmail, tempUser.password);
  await loginPage.submit();
  await userDashboardPage.waitFor();
});

test('forgot password', async ({ page, tempUser }) => {
  test.slow(); // Checking email and logging in repeatedly takes time
  // Request forgot password email
  let loginPage = await logout(page);
  const forgotPasswordPage = await loginPage.clickForgotPassword();
  await forgotPasswordPage.fillForm(tempUser.email);
  await forgotPasswordPage.submit();

  // Use reset password link
  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  const emailPage = await inboxPage.openEmail();
  const resetPasswordUrl = await emailPage.getFirstLanguageDepotUrl();
  expect(resetPasswordUrl).not.toBeNull();
  expect(resetPasswordUrl!).toContain('resetPassword');

  const pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickResetPassword();
  const newPage = await pagePromise;
  const resetPasswordPage = await new ResetPasswordPage(newPage).waitFor();

  const newPassword = randomUUID();
  await resetPasswordPage.fillForm(newPassword);
  await resetPasswordPage.submit();

  // Confirm new password works
  loginPage = await logout(page);
  await loginPage.fillForm(tempUser.email, newPassword);
  await loginPage.submit();
  await new UserDashboardPage(page).waitFor();

  // Verify email link has expired
  await page.goto(resetPasswordUrl!);
  loginPage = await new LoginPage(page).waitFor();
  await expect(loginPage.page.getByText('The email you clicked has expired')).toBeVisible();
});

test('register via new-user invitation email', async ({ page }) => {
  await loginAs(page.request, 'admin', defaultPassword);
  const adminPage = await new AdminDashboardPage(page).goto();
  const projectPage = await adminPage.openProject('Sena 3', 'sena-3');

  const uuid = randomUUID();

  const newEmail = `${uuid}@mailinator.com`;

  const addMemberModal = await projectPage.clickAddMember();
  await addMemberModal.emailField.fill(newEmail);
  await addMemberModal.selectEditorRole();
  await addMemberModal.submitButton.click();

  // Check invite link returnTo is relative path, not absolute
  const inboxPage = await getInbox(page, uuid).goto();
  const emailPage = await inboxPage.openEmail();
  const invitationUrl = await emailPage.getFirstLanguageDepotUrl();
  expect(invitationUrl).not.toBeNull();
  expect(invitationUrl!).toContain('register');
  expect(invitationUrl!).toContain('returnTo=')
  expect(invitationUrl!).not.toContain('returnTo=http')

  // No need to clean up temp user account as user was never created
});
