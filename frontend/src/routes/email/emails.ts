import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import NewAdmin from '$lib/email/NewAdmin.svelte';
import type {ComponentType} from 'svelte';
import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import PasswordChanged from '$lib/email/PasswordChanged.svelte';
import CreateAccountRequest from '$lib/email/CreateAccountRequest.svelte';
import CreateProjectRequest from '$lib/email/CreateProjectRequest.svelte';
import type {CreateProjectInput} from '$lib/gql/generated/graphql';

export const enum EmailTemplate {
    NewAdmin = 'NEW_ADMIN',
    ForgotPassword = 'FORGOT_PASSWORD',
    VerifyEmailAddress = 'VERIFY_EMAIL_ADDRESS',
    PasswordChanged = 'PASSWORD_CHANGED',
    CreateAccountRequest = 'CREATE_ACCOUNT_REQUEST',
    CreateProjectRequest = 'CREATE_PROJECT_REQUEST',
}

export const componentMap = {
    [EmailTemplate.ForgotPassword]: ForgotPassword,
    [EmailTemplate.NewAdmin]: NewAdmin,
    [EmailTemplate.VerifyEmailAddress]: VerifyEmailAddress,
    [EmailTemplate.PasswordChanged]: PasswordChanged,
    [EmailTemplate.CreateAccountRequest]: CreateAccountRequest,
    [EmailTemplate.CreateProjectRequest]: CreateProjectRequest,
} satisfies Record<EmailTemplate, ComponentType>;

interface EmailTemplatePropsBase<T extends EmailTemplate> {
    template: T;
    name: string;
    baseUrl?: string;
}

interface ForgotPasswordProps extends EmailTemplatePropsBase<EmailTemplate.ForgotPassword> {
    resetUrl: string;
}

interface NewAdminProps extends EmailTemplatePropsBase<EmailTemplate.NewAdmin> {
  adminName: string;
  adminEmail: string;
}

interface VerifyEmailAddressProps extends EmailTemplatePropsBase<EmailTemplate.VerifyEmailAddress> {
    verifyUrl: string;
    newAddress: boolean;
}

interface CreateAccountProps extends EmailTemplatePropsBase<EmailTemplate.CreateAccountRequest> {
  managerName: string;
  projectName: string;
  verifyUrl: string;
}

interface CreateProjectProps extends EmailTemplatePropsBase<EmailTemplate.CreateProjectRequest> {
    project: CreateProjectInput;
    user: { name: string, email: string };
}

export type EmailTemplateProps =
    ForgotPasswordProps
    | NewAdminProps
    | VerifyEmailAddressProps
    | CreateAccountProps
    | CreateProjectProps
    | EmailTemplatePropsBase<EmailTemplate>;
