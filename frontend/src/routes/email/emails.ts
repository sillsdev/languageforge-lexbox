import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import type {ComponentType} from 'svelte';
import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import PasswordChanged from '$lib/email/PasswordChanged.svelte';
import CreateProjectRequest from '$lib/email/CreateProjectRequest.svelte';
import type {CreateProjectInput} from '$lib/gql/generated/graphql';

export const enum EmailTemplate {
    ForgotPassword = 'FORGOT_PASSWORD',
    VerifyEmailAddress = 'VERIFY_EMAIL_ADDRESS',
    PasswordChanged = 'PASSWORD_CHANGED',
    CreateProjectRequest = 'CREATE_PROJECT_REQUEST',
}

export const componentMap = {
    [EmailTemplate.ForgotPassword]: ForgotPassword,
    [EmailTemplate.VerifyEmailAddress]: VerifyEmailAddress,
    [EmailTemplate.PasswordChanged]: PasswordChanged,
    [EmailTemplate.CreateProjectRequest]: CreateProjectRequest,
    // TODO: Create ProjectInviteEmail template
} satisfies Record<EmailTemplate, ComponentType>;

interface EmailTemplatePropsBase<T extends EmailTemplate> {
    template: T;
    name: string;
    baseUrl?: string;
}

interface ForgotPasswordProps extends EmailTemplatePropsBase<EmailTemplate.ForgotPassword> {
    resetUrl: string;
}

interface VerifyEmailAddressProps extends EmailTemplatePropsBase<EmailTemplate.VerifyEmailAddress> {
    verifyUrl: string;
    newAddress: boolean;
}

interface CreateProjectProps extends EmailTemplatePropsBase<EmailTemplate.CreateProjectRequest> {
    project: CreateProjectInput;
    user: { name: string, email: string };
}

export type EmailTemplateProps =
    ForgotPasswordProps
    | VerifyEmailAddressProps
    | CreateProjectProps
    | EmailTemplatePropsBase<EmailTemplate>;
