<script lang="ts" module>
  import {Schema, type Node} from 'prosemirror-model';
  import {gt} from 'svelte-i18n-lingui';

  const textSchema = new Schema({
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
        inline: true,
        whitespace: 'pre',
        toDOM: (node) => ['span', {title: gt`Writing system: ${node.attrs.richSpan.ws}`}, 0],
        parseDOM: [{tag: 'span'}],
        //richSpan is used to track the original span which was modified
        //this allows us to update the text property without having to map all the span properties into the schema
        attrs: {richSpan: {default: {text: ''}}}
      },
      doc: {content: 'span*', attrs: {richString: {default: {spans: []}}}}
    }
  });

  //matching the character used in FieldWorks
  //https://unicode-explorer.com/c/2028
  export const lineSeparator = '\u2028';
  const newLine = '\n';

</script>
<script lang="ts">
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {Label} from '$lib/components/ui/label';
  import InputShell from '../ui/input/input-shell.svelte';
  import {EditorView} from 'prosemirror-view';
  import {AllSelection, EditorState, TextSelection} from 'prosemirror-state';
  import {keymap} from 'prosemirror-keymap';
  import {baseKeymap} from 'prosemirror-commands';
  import {undo, redo, history} from 'prosemirror-history';
  import {onDestroy, onMount} from 'svelte';
  import {watch} from 'runed';
  import type {HTMLAttributes} from 'svelte/elements';
  import {mergeProps} from 'bits-ui';

  let {
    value = $bindable(),
    label,
    readonly = false,
    onchange = () => {},
    autofocus,
    ...rest
  }:
    {
      value: IRichString | undefined,
      label?: string,
      readonly?: boolean,
      onchange?: (value: IRichString) => void,
    } & HTMLAttributes<HTMLDivElement> = $props();

  let elementRef: HTMLElement | null = $state(null);
  let dirty = $state(false);
  let editor: EditorView | null = null;

  let lastInteractionWasKeyboard = false;

  onMount(() => {
    // docs https://prosemirror.net/docs/
    editor = new EditorView(elementRef, {
      state: newState(),
      dispatchTransaction: (transaction) => {
        if (!editor) return;
        const newState = editor.state.apply(transaction);
        if (!newState.doc.eq(editor.state.doc)) {
          //todo, eventually we might want to let the user edit span props, not sure if node attributes or marks are the correct way to handle that
          //I suspect marks is the right way though.
          if (!value) value = {spans: []};
          value.spans = newState.doc.children.map((child) => {
            const originalRichSpan = child.attrs.richSpan;
            return {...originalRichSpan, text: replaceNewLineWithLineSeparator(child.textContent)};
          });
          dirty = true;
        }
        editor.updateState(newState);
      },
      editable() {
        return !readonly;
      },
      handleDOMEvents: {
        'focus': onfocus,
        'blur': onblur,
      }
    });
    editor.dom.setAttribute('tabindex', '0');

    const abortController = new AbortController();
    window.addEventListener('keydown', () => lastInteractionWasKeyboard = true);
    window.addEventListener('mousedown', () => lastInteractionWasKeyboard = false);
    return () => {
      abortController.abort();
    };
  });

  function onfocus(editor: EditorView) {
    if (lastInteractionWasKeyboard) { // tabbed in
      editor.dispatch(editor.state.tr.setSelection(new AllSelection(editor.state.doc)));
    }
  }

  function onblur(editor: EditorView) {
    if (dirty && value) {
      onchange(value);
      dirty = false;
    }

    // Clear the selection
    window.getSelection()?.removeAllRanges();
    editor.dispatch(editor.state.tr.setSelection(TextSelection.create(editor.state.doc, 0)));
  }

  watch(() => readonly, () => {
    // Triggers a refresh immediately rather than when the user next interacts with the editor
    editor?.setProps({});
  });

  onDestroy(() => {
    editor?.destroy();
    editor = null;
  });

  $effect(() => {
    if (!editor) return;
    const newDoc = valueToDoc();
    if (newDoc.eq(editor.state.doc)) return;
    editor.updateState(newState(newDoc));
  });

  function newState(doc: Node = valueToDoc()): EditorState {
    return EditorState.create({
      schema: textSchema,
      doc,
      //don't move the cursor on state updates if we can avoid it
      selection: editor?.state.selection,
      plugins: [
        history(),
        keymap({
          'Mod-z': undo,
          'Mod-y': redo,
          'Shift-Enter': (state, dispatch) => {
            if (dispatch) dispatch(state.tr.insertText(newLine));
            return true;
          },
        }),
        keymap(baseKeymap)
      ]
    });
  }

  function valueToDoc(): Node {
    return textSchema.node('doc', {richString: value}, value?.spans
      .filter(s => s.text)
      .map(s => {
        return textSchema.node('span', {richSpan: s}, [textSchema.text(replaceLineSeparatorWithNewLine(s.text))]);
      }) ?? []);
  }

  //lcm expects line separators, but html does not render them, so we replace them with \n
  function replaceNewLineWithLineSeparator(text: string) {
    return text.replaceAll(newLine, lineSeparator);
  }
  function replaceLineSeparatorWithNewLine(text: string) {
    return text.replaceAll(lineSeparator, newLine);
  }
</script>
<style>
  :global(.ProseMirror) {
    flex-grow: 1;
    outline: none;
    cursor: text;
  }
  :global(.ProseMirror span) {
    border-bottom: 1px solid currentColor;
    margin: 2px;
    white-space: pre-wrap;
  }
</style>

{#if label}
  <div {...rest}>
    <Label>{label}</Label>
    <InputShell {autofocus} class="p-2 h-auto" bind:ref={elementRef}/>
  </div>
{:else}
  <InputShell {autofocus} {...mergeProps({ class: 'p-2 h-auto'}, rest)} bind:ref={elementRef}/>
{/if}
