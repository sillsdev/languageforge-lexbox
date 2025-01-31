<script lang="ts">
  import {Button, Drawer, SelectField, Switch, ThemeSwitch} from 'svelte-ux';
  import DevContent from './DevContent.svelte';
  import {type View, views} from '../entry-editor/view-data';
  import type {ViewSettings} from '../services/view-service';
  import {generateExternalChanges} from '../debug';
  import {mdiClose} from '@mdi/js';
  import ShowEmptyFieldsSwitch from './ShowEmptyFieldsSwitch.svelte';
  import type {LexboxFeatures} from '$lib/services/feature-service';

  export let activeView: View;
  export let viewSettings: ViewSettings;
  export let features: LexboxFeatures;
  export let open = false;
</script>

<Drawer bind:open placement="right" classes={{ root: 'w-[400px] max-w-full' }}>
  <div class="absolute right-2 top-2">
    <Button icon={mdiClose} on:click={() => open = false} class="float-right"/>
  </div>
  <div class="flex flex-col min-h-full gap-4 px-6 pt-8 pb-4 w-full font-semibold">
    <SelectField
      label="Fields"
      options={views.map((view) => ({ value: view, label: view.label, group: view.label }))}
      bind:value={activeView}
      classes={{root: 'view-select w-auto', options: 'view-select-options'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>

    <div class="h-10">
      <ShowEmptyFieldsSwitch bind:value={viewSettings.showEmptyFields} />
    </div>

    <div class="h-10">
      <!-- svelte-ignore a11y-label-has-associated-control -->
      <label class="flex gap-2 items-center text-sm">
        <ThemeSwitch /> Dark mode
      </label>
    </div>

    <div class="grow"></div>
    <DevContent>
      <div class="flex flex-col gap-4">
        Debug
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.write}/>
          Write
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={features.history}/>
          History
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
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
