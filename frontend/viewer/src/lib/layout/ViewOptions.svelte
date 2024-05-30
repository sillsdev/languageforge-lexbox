<script lang="ts">
  import { Drawer, SelectField, Switch } from "svelte-ux";
  import type { Writable } from "svelte/store";
  import { views } from "../config-data";
  import type { ViewConfig } from "../config-types";

  export let viewConfig: Writable<ViewConfig>;
  export let open = false;
</script>

<Drawer bind:open placement="right" classes={{ root: 'w-[400px]' }}>
  <div class="flex flex-col h-full gap-4 px-6 py-4 w-full font-semibold">
    <SelectField
      label="View"
      options={views.map((view) => ({ value: view, label: view.label }))}
      bind:value={$viewConfig.activeView}
      classes={{root: 'view-select w-auto'}}
      clearable={false}
      labelPlacement="top"
      clearSearchOnOpen={false}
      fieldActions={(elem) => /* a hack to disable typing/filtering */ {elem.readOnly = true; return [];}}
      search={() => /* a hack to always show all options */ Promise.resolve()}>
    </SelectField>
    <!-- svelte-ignore a11y-label-has-associated-control -->
    <label class="flex gap-2 items-center text-sm h-10">
      <Switch bind:checked={$viewConfig.showExtraFields}
        color="neutral" />
        Show extra/hidden fields
    </label>
    <!-- svelte-ignore a11y-label-has-associated-control -->
    <label class="flex gap-2 items-center text-sm h-10">
      <Switch bind:checked={$viewConfig.hideEmptyFields}
        color="neutral" />
        Hide empty fields
    </label>
    <div class="grow"></div>
    <div class="flex flex-col gap-4">
      Debug
      <!-- svelte-ignore a11y-label-has-associated-control -->
      <label class="flex gap-2 items-center text-sm h-10 text-warning">
        <Switch bind:checked={$viewConfig.readonly} />
        Readonly
      </label>
      <!-- svelte-ignore a11y-label-has-associated-control -->
      <label class="flex gap-2 items-center text-sm h-10 text-warning">
        <Switch bind:checked={$viewConfig.generateExternalChanges}
          color="warning" />
        Simulate conflicting changes
      </label>
    </div>
  </div>
</Drawer>

<style>
  :global(.view-select input) {
    cursor: pointer;
  }
</style>
