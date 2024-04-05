<script lang="ts">
  import { FormField, PlainInput, randomFormId } from '$lib/forms';
  import { _typeaheadSearch } from '$lib/gql/typeahead-queries';

  export let label: string;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFormId();
  export let autofocus = false;
  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  export let value: string;

  $: typeaheadResults = _typeaheadSearch(value);

</script>

<FormField {id} {label} {error} {autofocus} >
  <PlainInput {id} bind:value type="text" />
</FormField>

  {#await typeaheadResults}
    <span>awaiting results...</span>
  {:then users}
    <ul class="menu p-0">
    {#each users as user}
      <li class="p-0"><button class="whitespace-nowrap" on:click={() => value = user.email ?? user.username ?? ''}>{user.name} {user.email ? `<${user.email}>` : `(${user.username})`}</button></li>
    {/each}
    </ul>
  {/await}
