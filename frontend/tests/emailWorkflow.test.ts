import {TEST_TIMEOUT_2X, defaultPassword, elawaProjectId, testOrgId} from './envVars';
import {addUserToProject, deleteUser, getCurrentUserId, loginAs, logout, verifyTempUserEmail} from './utils/authHelpers';
import {AcceptInvitationPage} from './pages/acceptInvitationPage';
import {AdminDashboardPage} from './pages/adminDashboardPage';
import {EmailSubjects} from './email/email-page';
import {LoginPage} from './pages/loginPage';
import {ResetPasswordPage} from './pages/resetPasswordPage';
import {UserAccountSettingsPage} from './pages/userAccountSettingsPage';
import {UserDashboardPage} from './pages/userDashboardPage';
import {expect} from '@playwright/test';
import {randomUUID} from 'crypto';
import {test} from './fixtures';
import {MaildevMailbox} from './email/maildev-mailbox';
import {ProjectPage} from './pages/projectPage';
import {OrgPage} from './pages/orgPage';
import {ProjectRole} from '$lib/gql/types';

const userIdsToDelete: string[] = [];

test.afterEach(async ({ page }) => {
  if (userIdsToDelete.length > 0) {
    await loginAs(page.request, 'admin');
    for (const userId of userIdsToDelete) {
      await deleteUser(page.request, userId);
    }
    userIdsToDelete.splice(0);
  }
});

test('register, verify, update, verify email address', async ({ page, tempUser, mailboxFactory }) => {
  test.slow(); // Checking email and logging in repeatedly takes time
  await loginAs(page.request, tempUser.email, tempUser.password);
  const userDashboardPage = await new UserDashboardPage(page).goto();
  await userDashboardPage.emailVerificationAlert.assertPleaseVerify();

  // Request extra verification email and verify
  await userDashboardPage.emailVerificationAlert.clickResendEmail();
  await userDashboardPage.emailVerificationAlert.assertVerificationSent();

  let emailPage = await tempUser.mailbox.openEmail(page, EmailSubjects.VerifyEmail);
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
  const mailbox = await mailboxFactory();
  await userPage.goto();
  await userPage.fillEmail(mailbox.email);
  await userPage.clickSave();

  await userPage.emailVerificationAlert.assertRequestedToChange();

  // Verify new email address
  emailPage = await mailbox.openEmail(page, EmailSubjects.VerifyEmail);
  pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickVerifyEmail();
  newPage = await pagePromise;
  userPage = await new UserAccountSettingsPage(newPage).waitFor();

  await userPage.emailVerificationAlert.assertSuccessfullyUpdated();

  // Confirm new email address works
  const loginPage = await logout(page);
  await loginPage.fillForm(mailbox.email, tempUser.password);
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
  const emailPage = await tempUser.mailbox.openEmail(page, EmailSubjects.ForgotPassword);
  const resetPasswordUrl = await emailPage.getFirstLanguageDepotUrl();
  expect(resetPasswordUrl).not.toBeNull();
  expect(resetPasswordUrl).toContain('resetPassword');

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

test('register via new-user invitation email', async ({ page, mailboxFactory }) => {
  test.setTimeout(TEST_TIMEOUT_2X);

  await loginAs(page.request, 'admin');
  const adminPage = await new AdminDashboardPage(page).goto();
  const projectPage = await adminPage.openProject('Sena 3', 'sena-3');

  const uuid = randomUUID();

  const mailbox = await mailboxFactory();
  const newEmail = mailbox.email;

  const addMemberModal = await projectPage.clickAddMember();
  await addMemberModal.emailField.fill(newEmail);
  await addMemberModal.selectEditorRole();
  await addMemberModal.inviteCheckbox.check();
  await addMemberModal.submitButton.click();
  await page.locator(':text("has been sent an invitation email")').waitFor();

  // Check invite link returnTo is relative path, not absolute
  const emailPage = await mailbox.openEmail(page, EmailSubjects.ProjectInvitation);
  const invitationUrl = await emailPage.getFirstLanguageDepotUrl();
  expect(invitationUrl).not.toBeNull();
  expect(invitationUrl).toContain('acceptInvitation');
  expect(invitationUrl).toContain('returnTo=');
  expect(invitationUrl).not.toContain('returnTo=http');

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

test('ask to join project via new-project page', async ({ page, tempUser, tempUserInTestOrg }) => {
    test.setTimeout(TEST_TIMEOUT_2X);

    // First, set up new user to be manager of Elawa project (since it doesn't have one in default seed data)
    const manager = tempUser;
    await loginAs(page.request, manager.email, manager.password);
    let dashboardPage = await new UserDashboardPage(page).goto();

    // Must verify email before being made manager of a project
    await dashboardPage.emailVerificationAlert.assertPleaseVerify();
    let newPage = await verifyTempUserEmail(page, tempUserInTestOrg);

    // Add manager to Elawa project
    await loginAs(newPage.request, 'admin');
    await addUserToProject(page.request, manager.id, elawaProjectId, ProjectRole.Manager);

    const { name, email, password } = tempUserInTestOrg;

    await loginAs(page.request, email, password);
    dashboardPage = await new UserDashboardPage(page).goto();

    // Must verify email before being allowed to request project creation
    await dashboardPage.emailVerificationAlert.assertPleaseVerify();
    newPage = await verifyTempUserEmail(page, tempUserInTestOrg);
    dashboardPage = await new UserDashboardPage(newPage).goto();

    // Create project with similar name to Elawa
    const newProjectPage = await dashboardPage.clickCreateProject();
    await newProjectPage.fillForm({name: 'Elaw', code: 'xyz', purpose: 'Testing', organization: 'Test Org'});
    await expect(newProjectPage.extraProjectsDiv).toBeVisible();
    await expect(newProjectPage.askToJoinBtn).toBeDisabled();
    await newProjectPage.extraProjectsDiv.locator('#extra-projects-elawa-dev-flex').check();
    await expect(newProjectPage.askToJoinBtn).toBeEnabled();
    await newProjectPage.askToJoinBtn.click();
    await expect(newProjectPage.toast('has been sent to the project manager(s)')).toBeVisible();

    // Log in as manager, approve join request.
    await loginAs(page.request, 'manager');
    const emailPage = await manager.mailbox.openEmail(page, EmailSubjects.ProjectJoinRequest, `: ${name}`);
    const pagePromise = emailPage.page.context().waitForEvent('page');
    await emailPage.clickApproveRequest();
    newPage = await pagePromise;
    const elawaProjectPage = await new ProjectPage(newPage, 'Elawa', 'elawa-dev-flex').waitFor();
    await elawaProjectPage.modal.getByRole('button', {name: 'Add Member'}).click();
    await elawaProjectPage.goto();

    // Log in as temp user, should see Elawa project
    await loginAs(page.request, email, password);
    dashboardPage = await new UserDashboardPage(page).goto();
    await dashboardPage.openProject('Elawa', 'elawa-dev-flex');
});

test('ask to join project via project page', async ({ page, tempUser, tempUserInTestOrg }) => {
    test.setTimeout(TEST_TIMEOUT_2X);

    // First, set up new user to be manager of Elawa project (since it doesn't have one in default seed data)
    const manager = tempUser;
    await loginAs(page.request, manager.email, manager.password);
    let dashboardPage = await new UserDashboardPage(page).goto();

    // Must verify email before being made manager of a project
    await dashboardPage.emailVerificationAlert.assertPleaseVerify();
    let newPage = await verifyTempUserEmail(page, tempUserInTestOrg);

    // Add manager to Elawa project
    await loginAs(newPage.request, 'admin');
    await addUserToProject(page.request, manager.id, elawaProjectId, ProjectRole.Manager);

    const { name, email, password } = tempUserInTestOrg;

    await loginAs(page.request, email, password);
    dashboardPage = await new UserDashboardPage(page).goto();

    // Must verify email before being allowed to request project creation
    await dashboardPage.emailVerificationAlert.assertPleaseVerify();
    let emailPage = await tempUserInTestOrg.mailbox.openEmail(page, EmailSubjects.VerifyEmail);
    let pagePromise = emailPage.page.context().waitForEvent('page');
    await emailPage.clickVerifyEmail();
    newPage = await pagePromise;
    dashboardPage = await new UserDashboardPage(newPage).goto();

    // Get to Elawa project page via org page, then ask to join
    const testOrgPage = await new OrgPage(page, 'Test Org', testOrgId).goto();
    await testOrgPage.membersTab.click();
    await testOrgPage.projectsTab.click();
    const projectPage = await testOrgPage.openProject('Elawa', 'elawa-dev-flex');
    await projectPage.askToJoinButton.click();

    // Log in as manager, approve join request.
    await loginAs(page.request, 'manager');
    emailPage = await manager.mailbox.openEmail(page, EmailSubjects.ProjectJoinRequest, `: ${name}`);
    pagePromise = emailPage.page.context().waitForEvent('page');
    await emailPage.clickApproveRequest();
    newPage = await pagePromise;
    const elawaProjectPage = await new ProjectPage(newPage, 'Elawa', 'elawa-dev-flex').waitFor();
    await elawaProjectPage.modal.getByRole('button', {name: 'Add Member'}).click();
    await elawaProjectPage.goto();

    // Log in as temp user, should see Elawa project
    await loginAs(page.request, email, password);
    dashboardPage = await new UserDashboardPage(page).goto();
    await dashboardPage.openProject('Elawa', 'elawa-dev-flex');
});
