<script lang="ts">
  import { mdiMagnify } from "@mdi/js";
  import { Checkbox, Dialog, ListItem, TextField, cls } from "svelte-ux";
  import { fieldName } from "../i18n";
  import type {FieldConfig} from '../config-types';
  import {useCurrentView} from '../services/view-service';

  export let open = false;
  let fieldSearch = '';
  let currentView = useCurrentView();

  //todo list all fields
  $: filteredFields = ([] as FieldConfig[]).filter(
    (field) =>
      !fieldSearch || fieldName(field)?.toLocaleLowerCase().includes(fieldSearch.toLocaleLowerCase())
  );
</script>

<Dialog bind:open class="w-[700px]" classes={{title: 'px-4'}}>
  <div slot="title" class="font-normal">
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
          title={fieldName(field, $currentView.i18nKey)}
          subheading={`Type: ${field.type}. WS: ${field.ws}.`}
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
