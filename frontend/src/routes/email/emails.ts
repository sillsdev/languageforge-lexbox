import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import type { ComponentType } from 'svelte';
import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import PasswordChanged from '$lib/email/PasswordChanged.svelte';

export const enum EmailTemplate {
  ForgotPassword = 'ForgotPassword',
  VerifyEmailAddress = 'VerifyEmailAddress',
  PasswordChanged = 'PasswordChanged',
}

export const componentMap = {
  [EmailTemplate.ForgotPassword]: ForgotPassword,
  [EmailTemplate.VerifyEmailAddress]: VerifyEmailAddress,
  [EmailTemplate.PasswordChanged]: PasswordChanged,
} satisfies Record<EmailTemplate, ComponentType>;

interface EmailTemplatePropsBase<T extends EmailTemplate> {
  template: T;
  name: string;
}

interface ForgotPasswordProps extends EmailTemplatePropsBase<EmailTemplate.ForgotPassword> {
  resetUrl: string;
}

interface VerifyEmailAddressProps extends EmailTemplatePropsBase<EmailTemplate.VerifyEmailAddress> {
  verifyUrl: string;
}

export type EmailTemplateProps = ForgotPasswordProps | VerifyEmailAddressProps | EmailTemplatePropsBase<EmailTemplate>;
