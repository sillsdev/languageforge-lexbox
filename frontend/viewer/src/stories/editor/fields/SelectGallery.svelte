<script lang="ts">
  import {MultiSelect, Select} from '$lib/components/field-editors';
  import * as BitsSelect from '$lib/components/ui/select';
  import {Label} from '$lib/components/ui/label';

  type Option = {id: string; label: string; group?: string};

  const fewOptions: Option[] = ['Noun', 'Verb', 'Adjective', 'Adverb'].map((label) => ({id: label, label}));
  const manyOptions: Option[] = [
    ...['root', 'stem', 'bound root', 'bound stem', 'particle', 'phrase', 'clitic'].map((label) => ({id: label, label, group: 'Stems'})),
    ...['prefix', 'suffix', 'infix', 'circumfix', 'enclitic', 'proclitic'].map((label) => ({id: label, label, group: 'Affixes'})),
  ];
  const domains: Option[] = Array.from({length: 60}, (_, i) => {
    const label = `${Math.floor(i / 9) + 1}.${(i % 9) + 1} ${['Sky', 'Water', 'Plant', 'Animal', 'Body', 'Food', 'Tool'][i % 7]}`;
    return {id: label, label};
  });
  const publications: Option[] = ['Main Dictionary', 'School Dictionary', 'Children'].map((label) => ({id: label, label}));

  let singleFew = $state<Option | undefined>(fewOptions[1]);
  let singleMany = $state<Option | undefined>(manyOptions[9]);
  let multiFew = $state<Option[]>([publications[0]]);
  let multiMany = $state<Option[]>([domains[3], domains[20]]);
  let bitsSingle = $state('contains');
  let bitsMulti = $state(['en', 'fr']);

  let saves = $state<string[]>([]);
  function log(name: string, value: unknown) {
    saves = [`${name}: ${JSON.stringify(value)}`, ...saves].slice(0, 8);
  }

  const matchTypes = [
    {value: 'contains', label: 'Contains'},
    {value: 'starts', label: 'Starts with'},
    {value: 'ends', label: 'Ends with'},
    {value: 'equals', label: 'Equals'},
  ];
  const wss = [
    {value: 'en', label: 'Eng (en)'},
    {value: 'fr', label: 'Fra (fr)'},
    {value: 'seh', label: 'Sen (seh)'},
  ];
</script>

<div class="grid gap-6 max-w-md">
  <div class="grid gap-1.5">
    <Label>Single, few options (field editor)</Label>
    <Select bind:value={singleFew} options={fewOptions} idSelector="id" labelSelector="label" clearable="row"
      placeholder="No part of speech" onchange={(v) => log('single few', v?.label)} />
  </div>
  <div class="grid gap-1.5">
    <Label>Single, many options with groups (field editor)</Label>
    <Select bind:value={singleMany} options={manyOptions} idSelector="id" labelSelector="label" groupSelector={(o) => o.group ?? ''}
      drawerTitle="Morph type" onchange={(v) => log('single many', v?.label)} />
  </div>
  <div class="grid gap-1.5">
    <Label>Multi, few options (field editor)</Label>
    <MultiSelect bind:values={multiFew} options={publications} idSelector="id" labelSelector="label" sortValuesBy="optionOrder"
      onchange={(v) => log('multi few', v.map((o) => o.label))} />
  </div>
  <div class="grid gap-1.5">
    <Label>Multi, many options (field editor)</Label>
    <MultiSelect bind:values={multiMany} options={domains} idSelector="id" labelSelector="label" drawerTitle="Semantic domains"
      onchange={(v) => log('multi many', v.map((o) => o.label))} />
  </div>
  <div class="grid gap-1.5">
    <Label>Single (filter, bits Select)</Label>
    <BitsSelect.Root type="single" bind:value={bitsSingle}>
      <BitsSelect.Trigger>{matchTypes.find((m) => m.value === bitsSingle)?.label}</BitsSelect.Trigger>
      <BitsSelect.Content>
        <BitsSelect.Group>
          <BitsSelect.GroupHeading>Filter by</BitsSelect.GroupHeading>
          {#each matchTypes as m (m.value)}
            <BitsSelect.Item value={m.value} label={m.label} />
          {/each}
        </BitsSelect.Group>
      </BitsSelect.Content>
    </BitsSelect.Root>
  </div>
  <div class="grid gap-1.5">
    <Label>Multi (filter, bits Select)</Label>
    <BitsSelect.Root type="multiple" bind:value={bitsMulti}>
      <BitsSelect.Trigger>{bitsMulti.join(', ') || 'Writing system'}</BitsSelect.Trigger>
      <BitsSelect.Content>
        <BitsSelect.Group>
          <BitsSelect.GroupHeading>Writing systems</BitsSelect.GroupHeading>
          {#each wss as ws (ws.value)}
            <BitsSelect.Item value={ws.value} label={ws.label} />
          {/each}
        </BitsSelect.Group>
      </BitsSelect.Content>
    </BitsSelect.Root>
  </div>
  <div class="text-sm">
    <div class="font-semibold">Saves (field editors only; newest first)</div>
    {#each saves as s, i (saves.length - i)}
      <div class="font-mono text-xs">{s}</div>
    {:else}
      <div class="text-muted-foreground">None yet</div>
    {/each}
  </div>
</div>
