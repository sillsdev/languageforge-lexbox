<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import { expect, fn, userEvent, within } from 'storybook/test';
  import EntityEditorPrimitiveDecorator from './EntityEditorPrimitiveDecorator.svelte';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import type {IEntry} from '$lib/dotnet-types';
  import {fwliteStoryParameters} from '../../fwl-parameters';

  let entry: IEntry = $state({
    id: crypto.randomUUID(),
    lexemeForm: {
      'seh': 'Lexeme form',
    },
    citationForm: {
      'seh': 'Citation form',
    },
    literalMeaning: {
      'en': {
        spans: [
          { text: 'Literal meaning', ws: 'en' },
        ],
      },
      'pt': {
        spans: [
          { text: 'Significado literal', ws: 'pt' },
        ],
      },
    },
    note: {
      'en': {
        spans: [
          { text: 'Note in English', ws: 'en' },
        ],
      }
    },
    complexForms: [],
    complexFormTypes: [],
    components: [],
    publishIn: [],
    senses: [],
  });

  const { Story } = defineMeta({
    title: 'editor/entity-primitives/entry',
    component: EntryEditorPrimitive,
    parameters: fwliteStoryParameters({
      viewPicker: true,
      value: entry,
    }),
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      readonly: false,
      entry,
    },
  });
</script>

<Story
  name="In editor"
  decorators={[
    /* @ts-expect-error Bug in Storybook https://github.com/storybookjs/storybook/issues/29951 */
    () => EntityEditorPrimitiveDecorator,
  ]}
  play={async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const senInput = await canvas.findByLabelText(/(Lexeme form|Word) Sen/);
    await expect(senInput).toBeInTheDocument();
    await userEvent.type(senInput, ' new text');
    senInput.blur();
    await expect(args.onchange).toHaveBeenCalledOnce();
  }}
/>

<Story name="Raw" />
