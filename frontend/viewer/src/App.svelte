<script lang="ts">
  import {
    AppBar,
    Button,
    Checkbox,
    Dialog,
    Field,
    Icon,
    ListItem,
    SelectField,
    TextField,
    cls,
  } from 'svelte-ux';
  import { mdiMagnify, mdiCog, mdiChevronDown } from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import { firstDefVal, firstVal } from './lib/utils';
  import { allFields, views } from './lib/config-data';
  import { fieldName } from './lib/i18n';
  import { LexboxServiceProvider, LexboxServices } from './lib/services/service-provider';
  import type { LexboxApi } from './lib/services/lexbox-api';
  import { entries as mockEntries, writingSystems as mockWritingSystems } from './lib/entry-data';
  import type { IEntry, WritingSystems } from './lib/mini-lcm';
  import { setContext } from 'svelte';
  import { writable } from 'svelte/store';

  const demoValues = writable<{
    generateExternalChanges: boolean,
  }>({
    generateExternalChanges: false,
  });

  const activeView = writable<typeof views[number]['value']>(views[0].value);
  setContext('demoValues', demoValues);
  setContext('activeView', activeView);

  $: console.log(activeView);

  const lexboxApi = undefined; //window.lexbox.ServiceProvider.getService<LexboxApi>(LexboxServices.LexboxApi);

  const entriesPromise: Promise<IEntry[]> = lexboxApi?.GetEntries(undefined) ?? Promise.resolve(mockEntries);
  let wsPromise: Promise<WritingSystems> = lexboxApi?.GetWritingSystems() ?? Promise.resolve(mockWritingSystems);

    const writingSystems = writable<WritingSystems>();
  setContext('writingSystems', writingSystems);

  /* eslint-disable @typescript-eslint/no-floating-promises */
  wsPromise.then((ws) => writingSystems.set(ws));

  entriesPromise.then((entries) => {
    console.log(entries);
  });

  wsPromise.then((ws) => {
    console.log(ws);
  });
  /* eslint-enable @typescript-eslint/no-floating-promises */

  let showSearchDialog = false;
  let showConfigDialog = false;
  let fieldSearch = '';

  $: filteredFields = allFields.filter(
    (field) =>
      !fieldSearch || fieldName(field)?.toLocaleLowerCase().includes(fieldSearch.toLocaleLowerCase())
  );
</script>

<div class="min-h-full_ flex flex-col">
  <AppBar title="FLEx-Lite" class="bg-surface-300">
    <div class="flex-grow"></div>
    <Field
    classes={{input: 'my-1'}}
      on:click={() => (showSearchDialog = true)}
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      icon={mdiMagnify}>Search</Field
    >
    <div class="flex-grow"></div>
    <div slot="actions"></div>
  </AppBar>

  <main class="p-8 flex-grow flex flex-col">
    <div
      class="grid flex-grow"
      style="grid-template-columns: 2fr 4fr 1fr; grid-template-rows: auto 1fr;"
    >
      <h2 class="flex text-2xl font-semibold col-span-2">
        <div class="flex-grow"></div>
        <div class="mr-12 flex gap-4">
          <Checkbox bind:checked={$demoValues.generateExternalChanges}>
            Test CRDT field
          </Checkbox>
          <Field label="View">
            <select value="all" class="text-sm w-full outline-none appearance-none cursor-pointer bg-surface-100">
              <option value="all">Everything</option>
              <option value="WeSay">WeSay</option>
              <option value="Language Forge">Language Forge</option>
            </select>
            <span slot="append">
              <Icon data={mdiChevronDown} />
            </span>
          </Field>
          <SelectField label="View" clearable={false} labelPlacement="top" bind:value={$activeView} options={views}>
          </SelectField>
          <Button
            on:click={() => (showConfigDialog = true)}
            variant="outline"
            icon={mdiCog}
          />
        </div>
      </h2>
      <div class="ml-4 self-end">Overview</div>
      <div
        class="my-4 h-full grid grid-cols-subgrid flex-grow row-start-2 col-span-3"
      >
        {#await Promise.all([entriesPromise, wsPromise])}
          Loading...
        {:then [entries]}
          <Editor {entries} />
        {/await}
      </div>
    </div>
  </main>
</div>

<Dialog bind:open={showSearchDialog} class="w-[700px]">
  <div slot="title">
    <TextField
      autofocus
      placeholder="Search entries"
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      icon={mdiMagnify}
    />
  </div>
  <div>
    {#await Promise.all([entriesPromise, wsPromise])}
      Loading entries...
    {:then [entries]}
      {#each entries as entry}
        <ListItem
          title={firstVal(entry.lexemeForm)}
          subheading={firstDefVal(entry)}
          class={cls('cursor-pointer', 'hover:bg-accent-50')}
          noShadow
        />
      {/each}
    {/await}
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">actions</div>
</Dialog>

<Dialog bind:open={showConfigDialog} class="w-[700px]">
  <div slot="title">
    <TextField
      bind:value={fieldSearch}
      autofocus
      placeholder="Search fields"
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      icon={mdiMagnify}
    />
  </div>
  <div>
    {#each filteredFields as field}
      <label for={field.id} class="contents">
        <ListItem
          title={fieldName(field)}
          subheading={`Type: ${field.type}. WS: ${field.ws}.`}
          class={cls('cursor-pointer', 'hover:bg-accent-50')}
          noShadow>
          <div slot="actions">
            <Checkbox id={field.id} circle dense />
          </div>
        </ListItem>
      </label>
        {:else}
        <div class="mx-8 my-4">
          No matching fields
        </div>
    {/each}
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">actions</div>
</Dialog>
