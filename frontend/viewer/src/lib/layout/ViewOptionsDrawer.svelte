<script lang="ts">
  import { Drawer, SelectField, Switch } from "svelte-ux";
  import type { Readable, Writable } from "svelte/store";
  import { views } from "../config-data";
  import type { LexboxFeatures, ViewConfig, ViewOptions } from "../config-types";
  import DevContent from "./DevContent.svelte";
  import { getContext } from "svelte";

  export let options: Writable<ViewOptions>;
  export let features: Writable<LexboxFeatures>;
  export let open = false;

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>

<Drawer bind:open placement="right" classes={{ root: 'w-[400px] max-w-full' }}>
  <div class="flex flex-col h-full gap-4 px-6 py-4 w-full font-semibold">
    <SelectField
      label="Fields"
      options={views.map((view) => ({ value: view, label: view.label, group: view.label }))}
      bind:value={$options.activeView}
      classes={{root: 'view-select w-auto', options: 'view-select-options'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
    <!-- svelte-ignore a11y-label-has-associated-control -->
    <label class="flex gap-2 items-center text-sm h-10">
      <Switch bind:checked={$options.showExtraFields}
        color="neutral" />
        Show extra/hidden fields
    </label>
    {#if !$viewConfig.readonly}
      <!-- svelte-ignore a11y-label-has-associated-control -->
      <label class="flex gap-2 items-center text-sm h-10">
        <Switch bind:checked={$options.hideEmptyFields}
          color="neutral" />
          Hide empty fields
      </label>
    {/if}
    <div class="grow"></div>
    <DevContent>
      <div class="flex flex-col gap-4">
        Debug
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={$features.write} />
          Write
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={$features.history} />
          History
        </label>
        <!-- svelte-ignore a11y-label-has-associated-control -->
        <label class="flex gap-2 items-center text-sm h-10 text-warning">
          <Switch bind:checked={$options.generateExternalChanges}
            color="warning" />
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
