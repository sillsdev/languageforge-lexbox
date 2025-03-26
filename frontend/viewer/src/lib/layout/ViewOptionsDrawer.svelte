<script lang="ts">
  import {Button, Drawer, SelectField, Switch, ThemeSwitch} from 'svelte-ux';
  import DevContent from './DevContent.svelte';
  import {type View, views} from '$lib/views/view-data';
  import type {ViewSettings} from '$lib/views/view-service';
  import {generateExternalChanges} from '../debug';
  import {mdiClose} from '@mdi/js';
  import ShowEmptyFieldsSwitch from './ShowEmptyFieldsSwitch.svelte';
  import type {LexboxFeatures} from '$lib/services/feature-service';
  import ThemeSyncer from '$lib/ThemeSyncer.svelte';

  export let activeView: View;
  export let viewSettings: ViewSettings;
  export let features: LexboxFeatures;
  export let open = false;

  const isWebComponent = !!document.querySelector('lexbox-svelte')?.shadowRoot;
</script>

<ThemeSyncer />

<Drawer bind:open placement="right" classes={{ root: 'w-[400px] max-w-full' }}>
  <div class="absolute right-2 top-2">
    <Button icon={mdiClose} on:click={() => open = false} class="float-right"/>
  </div>
  <div class="flex flex-col min-h-full gap-4 px-6 pt-8 pb-4 w-full font-semibold">
    <SelectField
      label="Fields"
      options={views.map((view) => ({ value: view.label, label: view.label, group: view.label }))}
      value={activeView.label}
      on:change={({detail}) => {
        // We can't use the view itself as the value, because it gets stringified
        // and contains circular references
        const view = views.find((view) => view.label === detail.value);
        if (view)  activeView = view;
      }}
      classes={{root: 'view-select w-auto', options: 'view-select-options', field: { container: 'border-surface-content/20' }}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
    <div class="h-10">
      <ShowEmptyFieldsSwitch bind:value={viewSettings.showEmptyFields} />
    </div>

    {#if !isWebComponent}
      <div class="h-10">
        <label class="flex gap-2 items-center text-sm">
          <ThemeSwitch /> Dark mode
        </label>
      </div>
    {/if}

    <div class="grow"></div>
    <DevContent>
      <div class="flex flex-col gap-4">
        Debug
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.write}/>
          Write
        </label>
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.history}/>
          History
        </label>
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={$generateExternalChanges}
                  color="warning"/>
          Simulate conflicting changes
        </label>
      </div>
    </DevContent>
  </div>
</Drawer>

<style lang="postcss">
  :global(.view-select input) {
    cursor: pointer;
  }

  /* We set the group, because the SelectField started breaking when using objects as option values
  (because there's an #each keyed on <group-value.Tostring()> = <undefined-[Object object]> = duplicates).
  So, having a group fixes things :(.*/
  :global(.view-select-options .group-header) {
    display: none;
  }
</style>
