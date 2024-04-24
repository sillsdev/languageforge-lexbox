<script lang="ts">
  import {
    AppBar,
    Button,
    Checkbox,
    Dialog,
    Field,
    ListItem,
    SelectField,
    Switch,
    TextField,
    cls,
  } from 'svelte-ux';
  import { mdiMagnify, mdiCog } from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import { firstDefOrGlossVal, firstVal } from './lib/utils';
  import { allFields, views } from './lib/config-data';
  import { fieldName } from './lib/i18n';
  import { LexboxServices } from './lib/services/service-provider';
  import type { IEntry, LexboxApi, WritingSystems } from './lib/services/lexbox-api';
  import { setContext } from 'svelte';
  import { derived, writable } from 'svelte/store';
  import { deriveInitializedValue, type Initializable } from './lib/app-types';
  import { deriveAsync } from './lib/utils/time';

  const demoValues = writable<{
    generateExternalChanges: boolean,
    showExtraFields: boolean,
    hideEmptyFields: boolean,
  }>({
    generateExternalChanges: false,
    showExtraFields: false,
    hideEmptyFields: false,
  });

  const activeView = writable<typeof views[number]['value']>(views[0].value);
  setContext('demoValues', demoValues);
  setContext('activeView', activeView);

  const lexboxApi = window.lexbox.ServiceProvider.getService<LexboxApi>(LexboxServices.LexboxApi);

  // const entries = writable<Initializable<IEntry[]>>({ initialized: false });
  const search = writable<string>('bara');
  const entries = deriveAsync(search, (s) => lexboxApi.SearchEntries(s ?? '', { offset: 0, count: Infinity, order: '' }), undefined, 200);

  const writingSystems = writable<Initializable<WritingSystems>>({ initialized: false });
  setContext('writingSystems', deriveInitializedValue(writingSystems));
  lexboxApi.GetWritingSystems().then((ws) => writingSystems.set({
    value: ws,
    initialized: true,
  }));

  let showSearchDialog = false;
  let showConfigDialog = false;
  let fieldSearch = '';

  $: filteredFields = allFields($activeView).filter(
    (field) =>
      !fieldSearch || fieldName(field)?.toLocaleLowerCase().includes(fieldSearch.toLocaleLowerCase())
  );
</script>

<!-- svelte-ignore a11y-missing-content -->
<a id="top"></a>

<div class="flex flex-col">
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

  <main class="p-8 pt-4 flex-grow flex flex-col">
    <div
      class="grid flex-grow gap-x-8"
      style="grid-template-columns: 2fr 4fr 1fr; grid-template-rows: auto 1fr;"
    >
      <div class="flex items-end mr-8">
        <TextField
          bind:value={$search}
          placeholder="Filter..."
          class="flex-grow"
          icon={mdiMagnify} />
      </div>
      <h2 class="flex text-2xl font-semibold col-span-1">
        <div class="flex gap-4 items-end w-full">
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label class="flex gap-2 items-center text-sm h-10 text-warning">
            <Switch bind:checked={$demoValues.generateExternalChanges}
              color="warning" />
            Simulate conflicting changes
          </label>
          <div class="grow" />
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label class="flex gap-2 items-center text-sm h-10">
            <Switch bind:checked={$demoValues.showExtraFields}
              color="neutral" />
              Show extra/hidden fields
          </label>
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label class="flex gap-2 items-center text-sm h-10">
            <Switch bind:checked={$demoValues.hideEmptyFields}
              color="neutral" />
              Hide empty fields
          </label>
          <SelectField
            label="View"
            options={views}
            bind:value={$activeView}
            classes={{root: 'view-select w-auto'}}
            clearable={false}
            labelPlacement="top"
            clearSearchOnOpen={false}
            search={() => /* a hack to always show all options */ Promise.resolve()}>
          </SelectField>
          <Button
            classes={{root: 'aspect-square h-10'}}
            on:click={() => (showConfigDialog = true)}
            variant="outline"
            icon={mdiCog}
          />
        </div>
      </h2>
      <div class="ml-8 self-end">Overview</div>
      <div
        class="my-4 h-full grid grid-cols-subgrid flex-grow row-start-2 col-span-3"
      >
        {#if !$entries || !$writingSystems.initialized}
            Loading...
        {:else}
            <Editor entries={$entries} />
        {/if}
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
    {#if !$entries || !$writingSystems.initialized}
        Loading entries...
    {:else}
      {#each $entries as entry}
        <ListItem
        title={firstVal(entry.lexemeForm)}
        subheading={firstDefOrGlossVal(entry.senses[0])}
        class={cls('cursor-pointer', 'hover:bg-accent-50')}
        noShadow
      />
      {/each}
    {/if}
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
          title={fieldName(field, $activeView?.i18n)}
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

<style>
  :global(.view-select input) {
    cursor: pointer;
  }
</style>
