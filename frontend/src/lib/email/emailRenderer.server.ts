import { parse, walk } from 'svelte/compiler';

import type { ComponentType } from 'svelte';
import type {EmailTemplateProps} from '../../routes/email/emails';
import { LOCALE_CONTEXT_KEY } from '$lib/i18n';
import type { TemplateNode } from 'svelte/types/compiler/interfaces';
import mjml2html from 'mjml';
import { readable } from 'svelte/store';

type RenderResult = { head: string, html: string, css: string };
export type RenderEmailResult = { subject: string, html: string };

function getSubject(head: string): string {
  // using the Subject.svelte component a title should have been placed in the head.
  // we need to parse the head and find the title and extract the text
  const {html} = parse(head, { filename: 'file.html' });
  let subject: string | undefined;
  // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
  walk(html as Parameters<typeof walk>[0], {
    enter(node: TemplateNode, ..._) {
      if (node.type === 'Element' && node.name === 'title') {
        subject = node.children?.[0].data as string;
      }
    }
  } as Parameters<typeof walk>[1]);
  if (!subject) throw new Error('subject not found');
  return subject;
}

export function render(emailComponent: ComponentType, props: Omit<EmailTemplateProps, 'template'>, userLocale: string): RenderEmailResult {
  const context = new Map([[LOCALE_CONTEXT_KEY, readable(userLocale)]]);
  // eslint-disable-next-line
  const result: RenderResult = (emailComponent as any).render(props, { context }) as RenderResult;
  const mjmlResult = mjml2html(result.html, { validationLevel: 'soft' });
  if (mjmlResult.errors) {
    console.error(mjmlResult.errors);
  }
  return {
    html: mjmlResult.html,
    subject: getSubject(result.head)
  };
}
