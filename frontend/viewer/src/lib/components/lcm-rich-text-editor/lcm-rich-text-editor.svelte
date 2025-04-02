<script lang="ts" module>
  import {Schema, type Node} from 'prosemirror-model';

  const textSchema = new Schema({
    nodes: {
      text: {},
      span: {
        content: 'text*',
        inline: true,
        whitespace: 'pre',
        toDOM: () => ['pre', 0],
        parseDOM: [{tag: 'pre'}],
        //richSpan is used to track the original span which was modified
        //this allows us to update the text property without having to map all the span properties into the schema
        attrs: {richSpan: {default: {text: ''}}}
      },
      doc: {content: 'span*', attrs: {richString: {default: {spans: []}}}}
    }
  });

</script>
<script lang="ts">
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {Label} from '$lib/components/ui/label';
  import InputShell from '../ui/input/input-shell.svelte';
  import {EditorView} from 'prosemirror-view';
  import {EditorState} from 'prosemirror-state';
  import {keymap} from 'prosemirror-keymap';
  import {baseKeymap} from 'prosemirror-commands';
  import {undo, redo, history} from 'prosemirror-history';
  import {onMount} from 'svelte';

  let {
    value = $bindable(),
    label
  }:
    {
      value: IRichString,
      label?: string,
    } = $props();

  let elementRef: HTMLElement | null = $state(null);
  let editor: EditorView | null = null;
  onMount(() => {
    // docs https://prosemirror.net/docs/
    editor = new EditorView(elementRef, {
      state: newState(),
      dispatchTransaction: (transaction) => {
        if (!editor) return;
        const newState = editor.state.apply(transaction);
        //todo, eventually we might want to let the user edit span props, not sure if node attributes or marks are the correct way to handle that
        //I suspect marks is the right way though.
        value.spans = newState.doc.children.map((child) => {
          const originalRichSpan = child.attrs.richSpan;
          return {...originalRichSpan, text: child.textContent};
        });
        editor.updateState(newState);
      }
    });
  });
  $effect(() => {
    if (!editor) return;
    //change was likely made via the editor, not a prop update
    if (editor.state.doc.attrs.richString === value) return;
    editor.updateState(newState());
  });

  function newState() {
    return EditorState.create({
      schema: textSchema,
      doc: valueToDoc(),
      plugins: [
        history(),
        keymap({'Mod-z': undo, 'Mod-y': redo}),
        keymap({'Shift-Enter': (state, dispatch) => {
          if (dispatch) dispatch(state.tr.insertText('\n'));
          return true;
        }}),
        keymap(baseKeymap)
      ]
    });
  }

  function valueToDoc(): Node {
    return textSchema.node('doc', {richString: value}, value.spans.map(s => {
      return textSchema.node('span', {richSpan: s}, [textSchema.text(s.text)]);
    }));
  }
</script>
<style>
  :global(.ProseMirror) {
    flex-grow: 1;
    outline: none;
  }
  :global(.ProseMirror pre) {
    border-bottom: 1px solid currentColor;
    display: inline;
    margin: 2px;
  }
</style>
<div>
  <Label>{label}</Label>
  <InputShell class="p-2 h-auto" bind:ref={elementRef}/>
</div>
