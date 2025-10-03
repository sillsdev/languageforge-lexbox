<script lang="ts" module>
  import {Fragment, Slice, type Node} from 'prosemirror-model';
  import {cn} from '$lib/utils';
  import {textSchema} from './editor-schema';

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
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {findNextTabbable} from '$lib/utils/tabbable';
  import {inputVariants} from '../ui/input/input.svelte';

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
  // should not be state unless we catch errors. See onBlur.
  let dirty = false;
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
        class: inputVariants({class: 'min-h-10 h-auto pb-1.5'}),
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
      handlePaste(view, _event, slice) {
        if (view.state.doc.content.size === 0) {
          // if the field is cleared, the resulting selection breaks paste (on older devices at least)
          selectAll(view);
        }

        // When a user copies a whole field. It includes the trailing <br>.
        // Pasting it often causes errors, so we remove them.
        function withoutBrs(nodes: readonly Node[]): Node[] {
          return nodes
            .filter(node => node.type.name !== textSchema.nodes.br.name)
            .map(node => {
              if (node.isText) return node;
              return node.copy(Fragment.fromArray(withoutBrs(node.content.content)));
            });
        }
        const cleanFragment = Fragment.fromArray(withoutBrs(slice.content.content));
        const cleanSlice = new Slice(cleanFragment, slice.openStart, slice.openEnd);

        // Below code is copied from prosemirror's doPaste
        // https://github.com/ProseMirror/prosemirror-view/blob/381c163b0abde96cabd609a8c4fc72ed2891b0e1/src/input.ts#L624
        function sliceSingleNode(slice: Slice): Node | null {
            return slice.openStart == 0 && slice.openEnd == 0 && slice.content.childCount == 1 ? slice.content.firstChild : null;
        }
        let singleNode = sliceSingleNode(cleanSlice);
        let tr = singleNode
            ? view.state.tr.replaceSelectionWith(singleNode, false)
            : view.state.tr.replaceSelection(cleanSlice);
        view.dispatch(tr.scrollIntoView().setMeta('paste', true).setMeta('uiEvent', 'paste'));
        return true;
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
          // We can land here when the field gets cleared for some reason.
          // In that case fromJSON doesn't like the prevSelection (on older devices at least)
          if (editor.state.doc.content.size) {
            try {
              const prevSelectionForCurrentDoc = Selection.fromJSON(editor.state.doc, prevSelection.toJSON());
              setSelection(prevSelectionForCurrentDoc);
            } catch {
              console.warn('Could not restore previous selection', prevSelection.toJSON(), editor.state.doc);
            }
          }
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
    // we cannot set and $state variables here, because if this is called due to navigation
    // it's too late to update state and doing so will throw.
    // See stomp-safe-lcm-rich-text-editor for how we handle that case.
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
          // eslint-disable-next-line @typescript-eslint/naming-convention
          'Backspace': (state) => {
            // If the field is empty, backspace results in an error (on older devices at least)
            return state.doc.content.size === 0;
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
    } else {
      //mimic submit
      if (editor?.dom) {
        const form = editor.dom.closest('form');
        if (form) {
          form.requestSubmit();
        }
      }
    }
  }

  function valueToDoc(): Node {
    return textSchema.node('doc', {}, value?.spans
      .filter(s => s.text)
      .map((s, i, all) => richSpanToNode(s, i === all.length - 1)) ?? []);
  }

  function richSpanToNode(s: IRichSpan, isLast: boolean): Node {
    //we must pull text out of what is stored on the node attrs
    //ProseMirror will keep the text up to date itself, if we store it on the richSpan attr then it will become out of date
    let {text, ...rest} = s;
    //if the ws doesn't match expected, or there's more than just the ws key in props
    const isCustomized = (!!s.ws && !!normalWs && normalWs !== s.ws) || Object.keys(rest).length > 1;
    return textSchema.node('span', {richSpan: rest, className: cn(isCustomized && 'customized')}, [
      textSchema.text(replaceLineSeparatorWithNewLine(text)),
      // a <br> seems to be the only thing that will cause a trailing \n to be rendered
      // this is what Prose-Mirror does if inline: false, which we can't use
      ...(isLast ? [textSchema.node('br')] : [])
    ]);
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
</script>

<style lang="postcss" global>
  .ProseMirror {
    display: block !important; /*prevent display from being overridden by .flex in the inputVariant class */
    align-content: center;
    flex-grow: 1;
    outline: none;
    cursor: text;

    white-space: pre-wrap;

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

{#if label}
  <div class={className} {...rest}>
    <Label>{label}</Label>
    <div bind:this={elementRef}></div>
  </div>
{:else}
  <div bind:this={elementRef} class={className} {...rest}></div>
{/if}
