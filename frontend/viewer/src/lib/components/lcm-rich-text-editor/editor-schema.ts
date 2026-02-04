import {Schema} from 'prosemirror-model';
import {gt} from 'svelte-i18n-lingui';
import {cn} from '$lib/utils';
/*
  whitespace: pre tells prose-mirror how to parse the dom, not how to render it, that's our job
  */
export const textSchema = new Schema({
  nodes: {
    text: {
      whitespace: 'pre',
    },
    /**
     * Note: it seems that our spans likely should have been modeled as "marks" rather than "nodes".
     * Conceptually our users "mark" up a text.
     * I assume using marks would make delete and backspace behave more intuitively and automatically solve what our
     * custom key bindings do.
     * */
    span: {
      selectable: false,
      content: 'text* br?',
      // If we remove this Backspace + Delete start removing whole spans
      inline: true,
      whitespace: 'pre',
      toDOM: (node) => {
        return [
          'span',
          {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-argument,@typescript-eslint/no-unsafe-member-access
            title: gt`Writing system: ${node.attrs.richSpan.ws}`,
            // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
            class: cn(node.attrs.className),
          },
          0,
        ];
      },
      parseDOM: [{tag: 'span'}],
      //richSpan is used to track the original span which was modified
      //this allows us to update the text property without having to map all the span properties into the schema
      attrs: {richSpan: {default: {}}, className: {default: ''}},
    },
    br: {
      inline: true,
      group: 'inline',
      selectable: false,
      linebreakReplacement: true,
      toDOM: () => ['br'],
      parseDOM: [{tag: 'br'}],
    },
    doc: {
      whitespace: 'pre',
      // if we remove this Shift + Enter creates new spans and then Backspace starts removing whole spans
      inline: true,
      content: 'span*',
      attrs: {},
    },
  },
});
