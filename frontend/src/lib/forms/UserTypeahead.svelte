<script lang="ts">
  import { FormField, PlainInput, randomFormId } from '$lib/forms';
  import { _userTypeaheadSearch, _usersTypeaheadSearch, type SingleUserTypeaheadResult, type SingleUserICanSeeTypeaheadResult } from '$lib/gql/typeahead-queries';
  import { overlay } from '$lib/overlay';
  import { deriveAsync } from '$lib/util/time';
  import { writable } from 'svelte/store';
  import { createEventDispatcher } from 'svelte';

  export let label: string;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFormId();
  export let autofocus: true | undefined = undefined;
  export let value: string;
  export let debounceMs = 200;
  export let isAdmin: boolean = false;
  export let exclude: string[] = [];

  const input = writable('');
  $: $input = value;
  const typeaheadResults = deriveAsync(
    input,
    isAdmin ? _userTypeaheadSearch : _usersTypeaheadSearch,
    [],
    debounceMs);

  $: filteredResults = $typeaheadResults.filter(user => !exclude.includes(user.id));

  const dispatch = createEventDispatcher<{
      selectedUserChange: SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult | null;
  }>();

  let selectedUser = writable<SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult | null>(null);
  $: dispatch('selectedUserChange', $selectedUser);

  function selectUser(user: SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult): void {
    $selectedUser = user;
    $input = value = getInputValue(user);
  }

  $: if ($selectedUser && value !== getInputValue($selectedUser)) {
    $selectedUser = null;
  }

  function formatResult(user: SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult): string {
    const extra = 'username' in user && user.username && 'email' in user && user.email ? ` (${user.username}, ${user.email})`
                : 'username' in user && user.username ? ` (${user.username})`
                : 'email' in user && user.email ? ` (${user.email})`
                : '';
    return `${user.name}${extra}`;
  }

  function getInputValue(user: SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult): string {
    if ('email' in user && user.email) return user.email;
    if ('username' in user && user.username) return user.username;
    if ('name' in user && user.name) return user.name;
    return '';
  }

  let highlightIdx: number | undefined = undefined;
  let typeaheadOpen = false;
  $: {
    if (typeaheadOpen) {
      highlightIdx ??= filteredResults.length ? 0 : undefined;
    } else {
      highlightIdx = undefined;
    }
  }
  function keydownHandler(event: KeyboardEvent): void
  {
    if (!typeaheadOpen) return;
    if (!filteredResults.length) return;
    if (highlightIdx === undefined) return;

    const max = filteredResults.length - 1;

    switch (event.key) {
      case 'ArrowDown':
        highlightIdx = Math.min(max, highlightIdx + 1);
        event.preventDefault();
        break;
      case 'ArrowUp':
        highlightIdx = Math.max(0, highlightIdx - 1);
        event.preventDefault();
        break;
      case 'Enter':
        selectUser(filteredResults[highlightIdx]);
        event.preventDefault();
        break;
    }
  }

</script>

<FormField {id} {label} {error} {autofocus} >
  <div use:overlay={{ closeClickSelector: '.menu li'}}
    on:overlayOpen={(e) => typeaheadOpen = e.detail}>
    <PlainInput
      style="w-full"
      bind:value {id}
      type="text"
      autocomplete="off"
      {autofocus}
      {keydownHandler}
    />
    <div class="overlay-content">
      <ul class="menu p-0">
      {#each filteredResults as user, idx}
        <li class={(highlightIdx == idx) ? 'p-0 bg-primary text-white' : 'p-0'}><button class="whitespace-nowrap" on:click={() => {
          setTimeout(() => selectUser(user));
        }}>{formatResult(user)}</button></li>
      {/each}
      </ul>
    </div>
  </div>
</FormField>
