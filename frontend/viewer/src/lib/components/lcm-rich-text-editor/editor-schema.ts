import {Schema} from 'prosemirror-model';
import {gt} from 'svelte-i18n-lingui';
import {cn} from '$lib/utils';

export const textSchema = new Schema({
  nodes: {
    text: {},
    /**
     * Note: it seems that our spans likely should have been modeled as "marks" rather than "nodes".
     * Conceptually our users "mark" up a text.
     * I assume using marks would make delete and backspace behave more intuitively and automatically solve what our
     * custom key bindings do.
     * */
    span: {
      selectable: false,
      content: 'text*',
      whitespace: 'pre',
      toDOM: (node) => {
        return ['span', {
          title: gt`Writing system: ${node.attrs.richSpan.ws}`,
          class: cn(node.attrs.className),
        }, 0];
      },
      parseDOM: [{tag: 'span'}],
      //richSpan is used to track the original span which was modified
      //this allows us to update the text property without having to map all the span properties into the schema
      attrs: {richSpan: {default: {}}, className: {default: ''}}
    },
    doc: {content: 'span*', attrs: {}}
  }
});
