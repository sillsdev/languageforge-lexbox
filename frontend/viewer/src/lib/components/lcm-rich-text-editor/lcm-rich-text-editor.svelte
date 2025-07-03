<script lang="ts" module>
  import {type Node, Schema} from 'prosemirror-model';
  import {gt} from 'svelte-i18n-lingui';
  import {cn} from '$lib/utils';

  /*
  whitespace: pre tells prose-mirror how to parse the dom, not how to render it, that's our job
  */
  const textSchema = new Schema({
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
        content: 'text*',
        inline: true,
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
      doc: {
        whitespace: 'pre',
        inline: true,
        content: 'span*',
        attrs: {}
      }
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
  import {AllSelection, EditorState, Selection} from 'prosemirror-state';
  import {keymap} from 'prosemirror-keymap';
  import {baseKeymap} from 'prosemirror-commands';
  import {undo, redo, history} from 'prosemirror-history';
  import {onDestroy, onMount} from 'svelte';
  import {watch} from 'runed';
  import type {HTMLAttributes} from 'svelte/elements';
  import {IsUsingKeyboard} from 'bits-ui';
  import type {IRichSpan} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichSpan';
  import {on} from 'svelte/events';
  import InputShell from '../ui/input/input-shell.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {findNextTabbable} from '$lib/utils/tabbable';

  let {
    value = $bindable(),
    id,
    normalWs = undefined,
    'aria-labelledby': ariaLabelledby,
    'aria-label': ariaLabel,
    label,
    autocapitalize = 'off',
    readonly = false,
    onchange = () => {},
    autofocus,
    class: className,
    enterkeyhint = 'next',
    ...rest
  }:
    {
      value: IRichString | undefined,
      /**
       * when set, we will underline text not in this writing system
       */
      normalWs?: string,
      label?: string,
      readonly?: boolean,
      onchange?: (value: IRichString) => void,
    } & HTMLAttributes<HTMLDivElement> = $props();

  let elementRef: HTMLElement | null = $state(null);
  let dirty = $state(false);
  let editor: EditorView | null = null;

  const isUsingKeyboard = new IsUsingKeyboard();
  // isUsingKeyboard.current isn't entirely reliable on mobile due to virtual keyboards
  let pointerDown = false;

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
          value = { spans: newState.doc.children.map((child) => richSpanFromNode(child))
            .filter(s => s.text) };
          dirty = true;
        }
        editor.updateState(newState);
      },
      attributes: {
        // todo: the distribution of props between the editor and the elementRef is not good
        // there should probably be a wrapper component that provides the elementRef to this one
        ...(id ? {id} : {}),
        ...(ariaLabelledby ? {'aria-labelledby': ariaLabelledby} : {}),
        ...(ariaLabel ? {'aria-label': ariaLabel} : {}),
        role: 'textbox',
        ...(autocapitalize ? {autocapitalize} : {}),
        spellcheck: 'false',
        ...(enterkeyhint ? {enterkeyhint} : {}),
      },
      editable() {
        return !readonly;
      },
      handleDOMEvents: {
        pointerdown() {
          pointerDown = true;
          setTimeout(() => pointerDown = false, 100); // yes, apparently we need a decently high timeout value
        },
        'focus'(editor) {
          onfocus(editor, !pointerDown);
        },
        'blur': onblur,
        keydown(_view, event) {
          if (event.key === 'Enter') {
            onEnterKey();
          }
        },
      }
    });
    editor.dom.setAttribute('tabindex', '0');

    const relatedLabel = elementRef?.closest('label') ??
      (id ? document.querySelector<HTMLLabelElement>(`label[for="${id}"]`) : undefined);
    if (relatedLabel) return on(relatedLabel, 'click', onFocusTargetClick);
  });

  let prevSelection: Selection | undefined;
  function onfocus(editor: EditorView, viaKeyboard: boolean) {
    const usingKeyboard = isUsingKeyboard.current ||
      IsMobile.value && viaKeyboard;
    if (usingKeyboard) { // tabbed in
      if (IsMobile.value) {
        if (prevSelection) {
          const prevSelectionForCurrentDoc = Selection.fromJSON(editor.state.doc, prevSelection.toJSON());
          setSelection(prevSelectionForCurrentDoc);
          prevSelection = undefined;
        } else {
          setSelection(Selection.atEnd(editor.state.doc));
        }
      } else {
        selectAll(editor);
      }
    }
  }

  function onblur(editor: EditorView) {
    if (dirty && value) {
      onchange(value);
      dirty = false;
    }
    prevSelection = editor.state.selection;
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
          // eslint-disable-next-line @typescript-eslint/naming-convention
          'Enter': () => {
            // This handler is not triggered reliably on mobile, so we're using handleDOMEvents.keydown instead
            // if (IsMobile.value) onEnterKey();

            // and we never want to do anything on desktop
            // (partially because it causes range errors - on mobile too)
            return true;
          },
          'Shift-Enter': (state, dispatch) => {
            if (dispatch) dispatch(state.tr.insertText(newLine));
            return true;
          },
        }),
        keymap(baseKeymap)
      ]
    });
  }

  function onEnterKey(): void {
    if (IsMobile.value && enterkeyhint === 'next' && editor?.dom) {
      // mimic <input> 'next' behaviour
      const nextTabbable = findNextTabbable(editor.dom);
      nextTabbable?.focus();
    }
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
    //if the ws doesn't match expected, or there's more than just the ws key in props
    const isCustomized = (!!s.ws && !!normalWs && normalWs !== s.ws) || Object.keys(rest).length > 1;
    return textSchema.node('span', {richSpan: rest, className: cn(isCustomized && 'customized')}, [textSchema.text(replaceLineSeparatorWithNewLine(text))]);
  }

  function richSpanFromNode(node: Node) {
    return {...node.attrs.richSpan, text: replaceNewLineWithLineSeparator(node.textContent)};
  }

  function selectAll(editor: EditorView) {
    setSelection(new AllSelection(editor.state.doc));

    if (!readonly) return; // happens automatically if not readonly

    const selection = window.getSelection();
    if (!selection) return;
    const range = document.createRange();
    range.selectNodeContents(editor.dom);
    selection.removeAllRanges();
    selection.addRange(range);
  }

  function clearSelection(editor: EditorView) {
    const selection = window.getSelection();
    //only remove range when selection is this component, otherwise this will remove the selection from something else
    //this results in the cursor going to the start of the element
    if (selection && editor.dom.contains(selection.anchorNode) && editor.dom.contains(selection.focusNode)) {
      selection.removeAllRanges();
    }
  }

  function setSelection(selection: Selection): void {
    if (!editor) return;
    editor.dispatch(editor.state.tr.setSelection(selection));
    setTimeout(() => {
      if (!editor) return;
      if (editor.state.selection.eq(selection) || selection.$anchor.doc !== editor.state.doc) return;
      // when tabbing back and forth between 2 rich text editors, the previous setSelection is not sufficient 🤷
      // (It doesn't have anything to do with the clearSelection() below)
      // The first call is kept, because it avoid a visible delay when possible
      editor.dispatch(editor.state.tr.setSelection(selection));
    }, 1); // 0 is not enough on Firefox
  }

  //lcm expects line separators, but html does not render them, so we replace them with \n
  function replaceNewLineWithLineSeparator(text: string) {
    return text.replaceAll(newLine, lineSeparator);
  }
  function replaceLineSeparatorWithNewLine(text: string) {
    return text.replaceAll(lineSeparator, newLine);
  }

  function onFocusTargetClick(event: MouseEvent) {
    if (!editor || !event.target) return;
    if (event.target === editor.dom || event.target instanceof globalThis.Node && editor.dom.contains(event.target))
      return; // the editor will handle focus itself
    editor?.focus();
    // Minorly dissatisfying is that when you click on the padding to the right of prose-mirror,
    // the caret is not intelligently placed at the end of the text (and on the correct line if multi-line)
    // Everything else seems good
  }

  const wrapperClasses = 'px-3 min-h-10 h-max block overflow-hidden cursor-text place-content-center focus:ring-2';
</script>

<style lang="postcss" global>
  .ProseMirror {
    height: 100%;
    @apply py-1.5;
    place-content: center;
    flex-grow: 1;
    outline: none;
    cursor: text;
    overflow: auto;
    scrollbar-width: none;

    /* "pre" => only wrap for line breaks, NOT simply because the text doesn't have enough space */
    white-space: pre;

    :global(.customized) {
      text-decoration: underline;
    }

    :global(.customized ~ .customized) {
      margin-left: 2px;
    }

    &::before {
      /* Ensure baseline-alignment even works when empty */
      content: '\200B';
    }
  }
</style>

<!-- tabindex=-1 allows focus, but not tabbing, which makes focus:ring-2 work
when clicking around the real editor (just aesthetics) -->
{#if label}
  <div class={className} {...rest}>
    <Label>{label}</Label>
    <InputShell onclick={onFocusTargetClick}
      class={wrapperClasses}
      bind:ref={elementRef}
      tabindex={-1} />
  </div>
{:else}
  <InputShell onclick={onFocusTargetClick}
    class={cn(wrapperClasses, className)} bind:ref={elementRef}
    tabindex={-1}
    {...rest} />
{/if}
