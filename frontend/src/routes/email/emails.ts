import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import NewAdmin from '$lib/email/NewAdmin.svelte';
import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import PasswordChanged from '$lib/email/PasswordChanged.svelte';
import JoinProjectRequest from '$lib/email/JoinProjectRequest.svelte';
import CreateProjectRequest from '$lib/email/CreateProjectRequest.svelte';
import type {CreateProjectInput} from '$lib/gql/generated/graphql';
import ApproveProjectRequest from '$lib/email/ApproveProjectRequest.svelte';
import UserAdded from '$lib/email/UserAdded.svelte';
import CreateAccountRequestOrg from '$lib/email/CreateAccountRequestOrg.svelte';
import CreateAccountRequestProject from '$lib/email/CreateAccountRequestProject.svelte';


export const enum EmailTemplate {
    NewAdmin = 'NEW_ADMIN',
    ForgotPassword = 'FORGOT_PASSWORD',
    VerifyEmailAddress = 'VERIFY_EMAIL_ADDRESS',
    PasswordChanged = 'PASSWORD_CHANGED',
    CreateAccountRequestOrg = 'CREATE_ACCOUNT_REQUEST_ORG',
    CreateAccountRequestProject = 'CREATE_ACCOUNT_REQUEST_PROJECT',
    JoinProjectRequest = 'JOIN_PROJECT_REQUEST',
    CreateProjectRequest = 'CREATE_PROJECT_REQUEST',
    ApproveProjectRequest = 'APPROVE_PROJECT_REQUEST',
    UserAdded = 'USER_ADDED',
}

export const componentMap = {
    [EmailTemplate.ForgotPassword]: ForgotPassword,
    [EmailTemplate.NewAdmin]: NewAdmin,
    [EmailTemplate.VerifyEmailAddress]: VerifyEmailAddress,
    [EmailTemplate.PasswordChanged]: PasswordChanged,
    [EmailTemplate.CreateAccountRequestOrg]: CreateAccountRequestOrg,
    [EmailTemplate.CreateAccountRequestProject]: CreateAccountRequestProject,
    [EmailTemplate.JoinProjectRequest]: JoinProjectRequest,
    [EmailTemplate.CreateProjectRequest]: CreateProjectRequest,
    [EmailTemplate.ApproveProjectRequest]: ApproveProjectRequest,
    [EmailTemplate.UserAdded]: UserAdded,
} satisfies Record<EmailTemplate, any>;

interface EmailTemplatePropsBase<T extends EmailTemplate> {
    template: T;
    name: string;
    baseUrl?: string;
}

interface ForgotPasswordProps extends EmailTemplatePropsBase<EmailTemplate.ForgotPassword> {
    resetUrl: string;
    lifetime: string;
}

interface NewAdminProps extends EmailTemplatePropsBase<EmailTemplate.NewAdmin> {
  adminName: string;
  adminEmail: string;
}

interface VerifyEmailAddressProps extends EmailTemplatePropsBase<EmailTemplate.VerifyEmailAddress> {
    verifyUrl: string;
    newAddress: boolean;
    lifetime: string;
}

interface CreateAccountOrgProps extends EmailTemplatePropsBase<EmailTemplate.CreateAccountRequestOrg> {
  managerName: string;
  orgName: string;
  verifyUrl: string;
  lifetime: string;
}

interface CreateAccountProjectProps extends EmailTemplatePropsBase<EmailTemplate.CreateAccountRequestProject> {
  managerName: string;
  projectName: string;
  verifyUrl: string;
  lifetime: string;
}

interface JoinProjectProps extends EmailTemplatePropsBase<EmailTemplate.JoinProjectRequest> {
  managerName: string;
  requestingUserName: string;
  requestingUserId: string;
  projectCode: string;
  projectName: string;
}

interface CreateProjectProps extends EmailTemplatePropsBase<EmailTemplate.CreateProjectRequest> {
    project: CreateProjectInput;
    user: { name: string, email: string };
}

interface ApproveProjectProps extends EmailTemplatePropsBase<EmailTemplate.ApproveProjectRequest> {
  project: CreateProjectInput;
  user: { name: string, email: string };
}

interface UserAddedProps extends EmailTemplatePropsBase<EmailTemplate.UserAdded> {
  projectName: string;
  projectCode: string;
}

export type EmailTemplateProps =
    ForgotPasswordProps
    | NewAdminProps
    | VerifyEmailAddressProps
    | CreateAccountOrgProps
    | CreateAccountProjectProps
    | JoinProjectProps
    | CreateProjectProps
    | ApproveProjectProps
    | UserAddedProps
    | EmailTemplatePropsBase<EmailTemplate>;
