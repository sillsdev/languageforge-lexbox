import {render} from '$lib/email/emailRenderer.server';
import {json} from '@sveltejs/kit';
import type {RequestEvent} from './$types';
import {componentMap, EmailTemplate, type EmailTemplateProps} from './emails';
import type {Component} from 'svelte';

export async function POST(event: RequestEvent): Promise<Response> {
  const request = await event.request.json() as EmailTemplateProps;
  const {...props} = request;
  const component = componentMap[props.template] as unknown as Component<EmailTemplateProps>;
  if (!component) throw new Error(`Invalid email template ${props.template}.}`);
  return json(render(component, props, event.locals.activeLocale));
}


// just for testing
export function GET(event: RequestEvent): Response {
  const {type, ...props} = {
    type: EmailTemplate.ForgotPassword,
    name: 'John Doe',
    lifetime: '3 days',
    template: EmailTemplate.ForgotPassword,
    resetUrl: 'https://example.com/reset'
  };
  return json(render(componentMap[type] as unknown as Component<EmailTemplateProps>, props, event.locals.activeLocale));
}
