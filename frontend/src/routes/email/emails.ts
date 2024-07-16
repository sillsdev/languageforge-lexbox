import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import NewAdmin from '$lib/email/NewAdmin.svelte';
import type {ComponentType} from 'svelte';
import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import PasswordChanged from '$lib/email/PasswordChanged.svelte';
import CreateAccountRequestOrg from '$lib/email/CreateAccountRequestProject.svelte';
import CreateProjectRequest from '$lib/email/CreateProjectRequest.svelte';
import type {CreateProjectInput} from '$lib/gql/generated/graphql';
import ApproveProjectRequest from '$lib/email/ApproveProjectRequest.svelte';
import UserAdded from '$lib/email/UserAdded.svelte';
import CreateAccountRequestProject from '$lib/email/CreateAccountRequestProject.svelte';


export const enum EmailTemplate {
    NewAdmin = 'NEW_ADMIN',
    ForgotPassword = 'FORGOT_PASSWORD',
    VerifyEmailAddress = 'VERIFY_EMAIL_ADDRESS',
    PasswordChanged = 'PASSWORD_CHANGED',
    CreateAccountRequestOrg = 'CREATE_ACCOUNT_REQUEST_ORG',
    CreateAccountRequestProject = 'CREATE_ACCOUNT_REQUEST_PROJECT',
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
    [EmailTemplate.CreateProjectRequest]: CreateProjectRequest,
    [EmailTemplate.ApproveProjectRequest]: ApproveProjectRequest,
    [EmailTemplate.UserAdded]: UserAdded,
} satisfies Record<EmailTemplate, ComponentType>;

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

interface CreateAccountOrgProps extends EmailTemplatePropsBase<EmailTemplate.CreateAccountRequestProject> {
  managerName: string;
  orgName: string;
  verifyUrl: string;
  lifetime: string;
}

interface CreateAccountProjectProps extends EmailTemplatePropsBase<EmailTemplate.CreateAccountRequestOrg> {
  managerName: string;
  projectName: string;
  verifyUrl: string;
  lifetime: string;
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
    | CreateProjectProps
    | ApproveProjectProps
    | UserAddedProps
    | EmailTemplatePropsBase<EmailTemplate>;
