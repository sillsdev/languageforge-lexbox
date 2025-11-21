<script module lang="ts">
  import {Select} from '$lib/components/field-editors';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {fwliteStoryParameters} from '../../fwl-parameters';

  import {fn} from 'storybook/test';

  const partsOfSpeech = Object.freeze([
    { label: 'Noun' },
    { label: 'Verb' },
    { label: 'Adjective' },
    { label: 'Adverb' },
  ] as const);
  partsOfSpeech.forEach(Object.freeze);
  type PartOfSpeech = typeof partsOfSpeech[number];

  const partOfSpeech: PartOfSpeech = { label: 'Adjective' };

  const { Story } = defineMeta({
    component: Select<typeof partsOfSpeech[number]>,
    parameters: fwliteStoryParameters({ showValue: false }),
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      value: partOfSpeech,
      options: partsOfSpeech,
      readonly: false,
      idSelector: 'label',
      labelSelector: (item) => item.label,
      drawerTitle: `Part of speech`,
      filterPlaceholder: `Filter parts of speech...`,
      placeholder: `🤷 nothing here`,
      emptyResultsPlaceholder: `Looked hard, found nothing`,
    },
  });
</script>

<Story name="Standard" />
