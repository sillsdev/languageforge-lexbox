<script lang="ts">
  import type { WritingSystems } from "../mini-lcm";
  import type { Readable } from "svelte/store";
  import { getContext } from "svelte";
  import { pickWritingSystems } from "../utils";
  import type { FieldConfig } from "../types";
  import { fieldName } from "../i18n";
  import CrdtTextField from "./CrdtTextField.svelte";

  type T = $$Generic<{}>;
  export let field: FieldConfig;
  export let value: string;

  const allWritingSystems =
    getContext<Readable<WritingSystems>>("writingSystems");

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
</script>

<div class="single-field field">
  <span class="name w-32" title={field.id}>{fieldName(field)}</span>
  <div class="fields">
    <CrdtTextField on:change bind:value placeholder={ws.abbreviation} />
  </div>
</div>
