import { parse } from 'svelte/compiler';
import { render as svelte5Render } from 'svelte/server';
import { walk } from 'estree-walker';

import type { Component } from 'svelte';
import {EmailTemplate, type EmailTemplateProps} from '../../routes/email/emails';
import { LOCALE_CONTEXT_KEY } from '$lib/i18n';
import mjml2html from 'mjml';
import { readable } from 'svelte/store';

type RenderResult = { head: string, html: string };
export type RenderEmailResult = { subject: string, html: string };

function getSubject(head: string): string {
  // using the Subject.svelte component a title should have been placed in the head.
  // we need to parse the head and find the title and extract the text
  const {html} = parse(head, { filename: 'file.html', modern: false });
  // CAUTION: modern: true will become default in Svelte 6, at which point the node.type below will change to RegularElement
  let subject: string | undefined;
  // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
  walk(html as Parameters<typeof walk>[0], {
    enter(node, ..._) {
      if (node.type as string === 'Element' && 'name' in node && node.name === 'title') {
        if ('children' in node && Array.isArray(node.children))
        subject = node.children?.[0].data as string;
      }
    }
  } as Parameters<typeof walk>[1]);
  if (!subject) throw new Error('subject not found');
  console.log(`Subject: ${subject}`);
  return subject;
}

export function render(emailComponent: Component<EmailTemplateProps>, props: EmailTemplateProps, userLocale: string): RenderEmailResult {
  const context = new Map([[LOCALE_CONTEXT_KEY, readable(userLocale)]]);
  // eslint-disable-next-line
  const result: RenderResult = svelte5Render((emailComponent as any), { props, context });
  const mjmlResult = mjml2html(result.html, { validationLevel: 'soft' });
  if (mjmlResult.errors) {
    console.error(mjmlResult.errors);
  }
  return {
    html: mjmlResult.html,
    subject: getSubject(result.head)
  };
}
