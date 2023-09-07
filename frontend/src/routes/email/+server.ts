import { render } from '$lib/email/emailRenderer.server';
import { json } from '@sveltejs/kit';
import type { RequestEvent } from './$types';
import { componentMap, EmailTemplate, type EmailTemplateProps } from './emails';

export async function POST(event: RequestEvent): Promise<Response> {
  const request = await event.request.json() as EmailTemplateProps;
  const {template, ...props} = request;
  const component = componentMap[template];
  if (!component) throw new Error(`invalid template${template}`);
  return json(render(component, props));
}


// just for testing
export function GET(_event: RequestEvent): Response {
  const {type, ...props} = {
    type: EmailTemplate.ForgotPassword,
    name: 'John Doe',
    resetUrl: 'https://example.com/reset'
  };
  return json(render(componentMap[type], props));
}
