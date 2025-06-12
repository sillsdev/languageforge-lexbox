<script lang="ts" module>
  import {type Node, Schema} from 'prosemirror-model';
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
        whitespace: 'pre',
        toDOM: (node) => ['span', {title: gt`Writing system: ${node.attrs.richSpan.ws}`}, 0],
        parseDOM: [{tag: 'span'}],
        //richSpan is used to track the original span which was modified
        //this allows us to update the text property without having to map all the span properties into the schema
        attrs: {richSpan: {default: {}}}
      },
      doc: {content: 'span*', attrs: {}}
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
  import {EditorView} from 'prosemirror-view';
  import {AllSelection, EditorState} from 'prosemirror-state';
  import {keymap} from 'prosemirror-keymap';
  import {baseKeymap} from 'prosemirror-commands';
  import {undo, redo, history} from 'prosemirror-history';
  import {onDestroy, onMount} from 'svelte';
  import {watch} from 'runed';
  import type {HTMLAttributes} from 'svelte/elements';
  import {IsUsingKeyboard} from 'bits-ui';
  import type {IRichSpan} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichSpan';
  import {inputVariants} from '../ui/input/input.svelte';
  import {on} from 'svelte/events';

  let {
    value = $bindable(),
    id,
    'aria-labelledby': ariaLabelledby,
    'aria-label': ariaLabel,
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

  const isUsingKeyboard = new IsUsingKeyboard();

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
          value.spans = newState.doc.children.map((child) => richSpanFromNode(child))
            .filter(s => s.text);
          dirty = true;
        }
        editor.updateState(newState);
      },
      attributes: {
        class: inputVariants({class: 'min-h-10 h-auto block'}),
        // todo: the distribution of props between the editor and the elementRef is not good
        // there should probably be a wrapper component that provides the elementRef to this one
        ...(id ? {id} : {}),
        ...(ariaLabelledby ? {'aria-labelledby': ariaLabelledby} : {}),
        ...(ariaLabel ? {'aria-label': ariaLabel} : {}),
        role: 'textbox',
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

    const relatedLabel = elementRef?.closest('label') ??
      (id ? document.querySelector<HTMLLabelElement>(`label[for="${id}"]`) : undefined);
    if (relatedLabel) return on(relatedLabel, 'click', onFocusTargetClick);
  });

  function onfocus(editor: EditorView) {
    if (isUsingKeyboard.current) { // tabbed in
      selectAll(editor);
    }
  }

  function onblur(editor: EditorView) {
    if (dirty && value) {
      onchange(value);
      dirty = false;
    }
    clearSelection(editor);
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
    return textSchema.node('doc', {}, value?.spans
      .filter(s => s.text)
      .map(s => richSpanToNode(s)) ?? []);
  }

  function richSpanToNode(s: IRichSpan) {
    //we must pull text out of what is stored on the node attrs
    //ProseMirror will keep the text up to date itself, if we store it on the richSpan attr then it will become out of date
    let {text, ...rest} = s;
    return textSchema.node('span', {richSpan: rest}, [textSchema.text(replaceLineSeparatorWithNewLine(text))]);
  }

  function richSpanFromNode(node: Node) {
    return {...node.attrs.richSpan, text: replaceNewLineWithLineSeparator(node.textContent)};
  }

  function selectAll(editor: EditorView) {
    editor.dispatch(editor.state.tr.setSelection(new AllSelection(editor.state.doc)));
  }

  function clearSelection(editor: EditorView) {
    const selection = window.getSelection();
    //only remove range when selection is this component, otherwise this will remove the selection from something else
    //this results in the cursor going to the start of the element
    if (selection && editor.dom.contains(selection.anchorNode) && editor.dom.contains(selection.focusNode)) {
      selection.removeAllRanges();
    }
  }

  //lcm expects line separators, but html does not render them, so we replace them with \n
  function replaceNewLineWithLineSeparator(text: string) {
    return text.replaceAll(newLine, lineSeparator);
  }
  function replaceLineSeparatorWithNewLine(text: string) {
    return text.replaceAll(lineSeparator, newLine);
  }

  function onFocusTargetClick(event: MouseEvent) {
    if (!editor) return;
    if (event.target === editor?.dom) return; // the editor will handle focus itself
    editor.focus();
  }
</script>
<style>
  :global(.ProseMirror) {
    flex-grow: 1;
    outline: none;
    cursor: text;
    /*white-space must be here, if it's directly on span then it will crash with a null node error*/
    white-space: pre-wrap;
  }
  :global(.ProseMirror span) {
    border-bottom: 1px solid currentColor;
    margin: 2px;
  }
</style>

{#if label}
  <div {...rest}>
    <Label onclick={onFocusTargetClick}>{label}</Label>
    <div bind:this={elementRef}></div>
  </div>
{:else}
  <div bind:this={elementRef} {...rest}></div>
{/if}
