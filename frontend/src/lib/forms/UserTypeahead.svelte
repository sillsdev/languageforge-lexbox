<script lang="ts">
  import { FormField, PlainInput, randomFormId } from '$lib/forms';
  import { _userTypeaheadSearch, _orgMemberTypeaheadSearch, type SingleUserTypeaheadResult, type SingleUserInMyOrgTypeaheadResult } from '$lib/gql/typeahead-queries';
  import { overlay } from '$lib/overlay';
  import { deriveAsync } from '$lib/util/time';
  import { derived, writable } from 'svelte/store';
  import { createEventDispatcher } from 'svelte';

  export let label: string;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFormId();
  export let autofocus = false;
  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  export let value: string;
  export let debounceMs = 200;
  export let isAdmin: boolean = false;

  let input = writable('');
  $: $input = value;
  let typeaheadResults = deriveAsync(
    input,
    isAdmin ? _userTypeaheadSearch : _orgMemberTypeaheadSearch,
    [],
    debounceMs);

  const dispatch = createEventDispatcher<{
      selectedUserId: string | null;
      selectedUser: SingleUserTypeaheadResult | SingleUserInMyOrgTypeaheadResult | null;
  }>();

  let selectedUser = writable<SingleUserTypeaheadResult | SingleUserInMyOrgTypeaheadResult | null>(null);
  let selectedUserId = derived(selectedUser, user => user?.id ?? null);
  $: dispatch('selectedUserId', $selectedUserId);
  $: dispatch('selectedUser', $selectedUser);

  function formatResult(user: SingleUserTypeaheadResult | SingleUserInMyOrgTypeaheadResult): string {
    const extra = 'username' in user && user.username && 'email' in user && user.email ? ` (${user.username}, ${user.email})`
                : 'username' in user && user.username ? ` (${user.username})`
                : 'email' in user && user.email ? ` (${user.email})`
                : '';
    return `${user.name}${extra}`;
  }

  function getInputValue(user: SingleUserTypeaheadResult | SingleUserInMyOrgTypeaheadResult): string {
    if ('email' in user && user.email) return user.email;
    if ('username' in user && user.username) return user.username;
    if ('name' in user && user.name) return user.name;
    return '';
  }

</script>

<FormField {id} {label} {error} {autofocus} >
  <div use:overlay={{ closeClickSelector: '.menu li'}}>
    <PlainInput
      style="w-full"
      bind:value {id}
      type="text"
      autocomplete="off"
      keydownHandler={() => {$selectedUser = null}}
    />
    <div class="overlay-content">
      <ul class="menu p-0">
      {#each $typeaheadResults as user}
        <li class="p-0"><button class="whitespace-nowrap" on:click={() => {
          setTimeout(() => {
            if ('id' in user && user.id) {
              $selectedUser = user;
            }
            $input = value = getInputValue(user);
          });
        }}>{formatResult(user)}</button></li>
      {/each}
      </ul>
    </div>
  </div>
</FormField>
