import {parse, walk} from "svelte/compiler";
import mjml2html from 'mjml';
import type {TemplateNode} from "svelte/types/compiler/interfaces";

type RenderResult = { head: string, html: string, css: string };

function getSubject(head: string): string {
    // using the Subject.svelte component a title should have been placed in the head.
    // we need to parse the head and find the title and extract the text
    const headAst = parse(head, {filename: 'file.html'});
    let subject: string | undefined;
    walk(headAst.html, {
        enter(node: TemplateNode, parent, prop, index) {
            if (node.type === 'Element' && node.name === 'title') {
                subject = node.children?.[0].data;
            }
        }
    } as Parameters<typeof walk>[1]);
    if (!subject) throw new Error('subject not found');
    return subject;
}

export function render(emailComponent: any, props: Record<string, any>): { subject: string; html: string } {
    const result: RenderResult = emailComponent.render(props);
    return {
        html: mjml2html(result.html, {validationLevel: 'strict'}).html,
        subject: getSubject(result.head)
    };
}