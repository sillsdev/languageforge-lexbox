import VerifyEmailAddress from '$lib/email/VerifyEmailAddress.svelte';
import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import { render } from '$lib/email/emailRenderer.server';
import { EmailTemplate, componentMap } from '../../emails';
import type { PageServerLoad } from './$types';

export const load = (({ url }): { subject: string, html: string } | void => {
  const template = url.searchParams.get('type');
  switch (template) {
    case EmailTemplate.ForgotPassword:
      return render(ForgotPassword, { name: 'John Doe', resetUrl: 'https://garfield.com' });
    case EmailTemplate.VerifyEmailAddress:
      return render(VerifyEmailAddress, { name: 'John Doe', verifyUrl: 'https://google.com' });
  }

  if (template) {
    const email = componentMap[template as EmailTemplate];
    if (email) return render(email, { name: 'John Doe 😺' });
    return { subject: `Email type "${template}" doesn't have a component configured. 😿`, html: '' };
  }
}) satisfies PageServerLoad
