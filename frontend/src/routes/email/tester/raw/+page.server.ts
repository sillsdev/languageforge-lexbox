import { render } from '$lib/email/emailRenderer.server';
import { EmailTemplate, componentMap } from '../../emails';
import type { PageServerLoad } from './$types';

export const load = (({ url }): { subject: string, html: string } => {
  const params = {
    name: 'John Doe 😺',
    ...Object.fromEntries(url.searchParams.entries()),
  };
  const template = url.searchParams.get('type') as EmailTemplate;
  if (template) {
    const email = componentMap[template];
    if (email) return render(email, params);
    return { subject: `Email type "${template}" doesn't have a component configured. 😿`, html: '' };
  }

  return { subject: `Email type missing. 😿`, html: '' };
}) satisfies PageServerLoad
