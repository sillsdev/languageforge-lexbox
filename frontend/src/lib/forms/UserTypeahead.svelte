<script lang="ts">
  import { FormField, PlainInput } from '$lib/forms';
  import IconButton from '$lib/components/IconButton.svelte';
  import { _typeaheadSearch, type SingleUserTypeaheadResult } from '$lib/gql/typeahead-queries';

  export let label: string;
  export let autofocus = false;
  export let result: SingleUserTypeaheadResult;

  let typeaheadInput = '';
  $: typeaheadResults = _typeaheadSearch(typeaheadInput);

</script>

<FormField {label} {autofocus} description={result?.email ?? result?.username ?? ''}>
  {#if result}
  <div class="flex items-center gap-2">
    <PlainInput value={result.name} type="text" readonly />
    <IconButton icon="i-mdi-close"
      on:click={() => result = undefined}
      disabled={!result} />
  </div>
  {:else}
  <PlainInput bind:value={typeaheadInput} type="text" />
  {/if}
</FormField>
{#if !result}
  {#await typeaheadResults}
    <span>awaiting results...</span>
  {:then users}
    <ul>
    {#each users as user}
      <li><a href="" on:click={() => result = user}>{user.name} {user.email ? `<${user.email}>` : `(${user.username})`}</a></li>
    {/each}
    </ul>
  {/await}
{/if}
