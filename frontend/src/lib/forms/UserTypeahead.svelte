<script lang="ts">
  import { FormField, PlainInput, randomFormId } from '$lib/forms';
  import { _userTypeaheadSearch, type SingleUserTypeaheadResult } from '$lib/gql/typeahead-queries';
  import { overlay } from '$lib/overlay';
  import { deriveAsync } from '$lib/util/time';
  import { writable } from 'svelte/store';

  export let label: string;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFormId();
  export let autofocus = false;
  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  export let value: string;
  export let debounceMs = 200;

  let input = writable('');
  $: $input = value;

  let typeaheadResults = deriveAsync(input, _userTypeaheadSearch, [], debounceMs);

  function formatResult(user: SingleUserTypeaheadResult): string {
    const extra = user.username && user.email ? ` (${user.username}, ${user.email})`
                : user.username ? ` (${user.username})`
                : user.email ? ` (${user.email})`
                : '';
    return `${user.name}${extra}`;
  }

</script>

<FormField {id} {label} {error} {autofocus} >
  <div use:overlay={{ closeClickSelector: '.menu li'}}>
    <PlainInput style="w-full" {id} bind:value type="text" autocomplete="off" />
    <div class="overlay-content">
      <ul class="menu p-0">
      {#each $typeaheadResults as user}
        <li class="p-0"><button class="whitespace-nowrap" on:click={() => setTimeout(() => $input = value = user.email ?? user.username ?? '')}>{formatResult(user)}</button></li>
      {/each}
      </ul>
    </div>
  </div>
</FormField>
