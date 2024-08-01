import { TEST_TIMEOUT_2X, defaultPassword } from './envVars';
import { deleteUser, getCurrentUserId, loginAs, logout } from './utils/authHelpers';

import { AdminDashboardPage } from './pages/adminDashboardPage';
import { EmailSubjects } from './pages/mailPages';
import { LoginPage } from './pages/loginPage';
import { AcceptInvitationPage } from './pages/acceptInvitationPage';
import { ResetPasswordPage } from './pages/resetPasswordPage';
import { UserAccountSettingsPage } from './pages/userAccountSettingsPage';
import { UserDashboardPage } from './pages/userDashboardPage';
import { expect } from '@playwright/test';
import { getInbox } from './utils/mailboxHelpers';
import { randomUUID } from 'crypto';
import { test } from './fixtures';

const userIdsToDelete: string[] = [];

test.afterEach(async ({ page }) => {
  if (userIdsToDelete.length > 0) {
    await loginAs(page.request, 'admin', defaultPassword);
    for (const userId of userIdsToDelete) {
      await deleteUser(page.request, userId);
    }
    userIdsToDelete.splice(0);
  }
});

test('register, verify, update, verify email address', async ({ page, tempUser }) => {
  test.slow(); // Checking email and logging in repeatedly takes time
  await loginAs(page.request, tempUser.email, tempUser.password);
  const userDashboardPage = await new UserDashboardPage(page).goto();
  await userDashboardPage.emailVerificationAlert.assertPleaseVerify();

  // Request extra verification email and verify
  await userDashboardPage.emailVerificationAlert.clickResendEmail();
  await userDashboardPage.emailVerificationAlert.assertVerificationSent();

  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  let emailPage = await inboxPage.openEmail(EmailSubjects.VerifyEmail);
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
  emailPage = await inboxPage.openEmail(EmailSubjects.VerifyEmail);
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
  await page.locator(':text("Check Your Inbox")').first().waitFor();

  // Use reset password link
  const inboxPage = await getInbox(page, tempUser.mailinatorId).goto();
  const emailPage = await inboxPage.openEmail(EmailSubjects.ForgotPassword);
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
  test.setTimeout(TEST_TIMEOUT_2X);

  await loginAs(page.request, 'admin', defaultPassword);
  const adminPage = await new AdminDashboardPage(page).goto();
  const projectPage = await adminPage.openProject('Sena 3', 'sena-3');

  const uuid = randomUUID();

  const newEmail = `${uuid}@mailinator.com`;

  const addMemberModal = await projectPage.clickAddMember();
  await addMemberModal.emailField.fill(newEmail);
  await addMemberModal.selectEditorRole();
  await addMemberModal.checkBox.check();
  await addMemberModal.submitButton.click();
  await page.locator(':text("has been sent an invitation email")').waitFor();

  // Check invite link returnTo is relative path, not absolute
  const inboxPage = await getInbox(page, uuid).goto();
  const emailPage = await inboxPage.openEmail(EmailSubjects.ProjectInvitation);
  const invitationUrl = await emailPage.getFirstLanguageDepotUrl();
  expect(invitationUrl).not.toBeNull();
  expect(invitationUrl!).toContain('acceptInvitation');
  expect(invitationUrl!).toContain('returnTo=');
  expect(invitationUrl!).not.toContain('returnTo=http');

  // Click invite link, verify register page contains pre-filled email address
  const pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickFirstLanguageDepotUrl();
  const newPage = await pagePromise;
  const acceptPage = await new AcceptInvitationPage(newPage).waitFor();
  await expect(newPage.getByLabel('Email')).toHaveValue(newEmail);
  await acceptPage.fillForm(`Test user ${uuid}`, defaultPassword);

  await acceptPage.submit();
  const userDashboardPage = await new UserDashboardPage(newPage).waitFor();

  // Register current user ID to be cleaned up even if test fails later on
  const userId = await getCurrentUserId(newPage.request);
  userIdsToDelete.push(userId);

  // Should be able to open sena-3 project from user dashboard as we are now a member
  await userDashboardPage.openProject('Sena 3', 'sena-3');
});
