<script module lang="ts">
  import {MultiSelect} from '$lib/components/field-editors';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {fn} from 'storybook/test';

  const allDomains = [
    { label: 'fruit' }, { label: 'tree' }, { label: 'stars' }, { label: 'earth' },
  ].map(domain => Object.freeze(domain));
  for (let i = 0; i < 100; i++) {
    allDomains.push({
      label: allDomains[Math.floor(Math.random() * allDomains.length)].label + '-' + i
    });
  }
  allDomains.forEach(Object.freeze);
  allDomains.sort((a, b) => a.label.localeCompare(b.label));
  const readonlyDomains = Object.freeze(allDomains);

  function randomSemanticDomainSorter() {
    return Math.random() - 0.5;
  }

  const selectedDomains = [allDomains[0], allDomains[80]];

  const { Story } = defineMeta({
    component: MultiSelect<typeof allDomains[number]>,
    argTypes: {
      readonly: {
        control: { type: 'boolean' },
      },
    },
    args: {
      onchange: fn(),
      values: selectedDomains,
      options: readonlyDomains,
      readonly: false,
      idSelector: 'label',
      labelSelector: (item) => item.label,
      drawerTitle: `Semantic domains`,
      filterPlaceholder: `Filter semantic domains...`,
      placeholder: `🤷 nothing here`,
      emptyResultsPlaceholder: `Looked hard, found nothing`,
    },
  });
</script>

<Story name="Standard" />

<Story name="Option order"  args={{ sortValuesBy: 'optionOrder' }} />

<Story name="Random order" args={{ sortValuesBy: randomSemanticDomainSorter }} />
