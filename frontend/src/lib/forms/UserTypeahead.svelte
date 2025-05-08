<script lang="ts">
  import { run } from 'svelte/legacy';

  import { FormField, PlainInput, randomFormId } from '$lib/forms';
  import { _userTypeaheadSearch, _usersTypeaheadSearch, type SingleUserTypeaheadResult, type SingleUserICanSeeTypeaheadResult } from '$lib/gql/typeahead-queries';
  import { overlay } from '$lib/overlay';
  import { deriveAsync } from '$lib/util/time';
  import { writable } from 'svelte/store';
  import { createEventDispatcher } from 'svelte';

  type UserTypeaheadResult = SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult;

  interface Props {
    label: string;
    error?: string | string[];
    id?: string;
    autofocus?: true;
    value: string;
    debounceMs?: number;
    isAdmin?: boolean;
    exclude?: string[];
  }

  let {
    label,
    error,
    id = randomFormId(),
    autofocus,
    value = $bindable(),
    debounceMs = 200,
    isAdmin = false,
    exclude = []
  }: Props = $props();

  function typeaheadSearch(): Promise<UserTypeaheadResult[]> {
    return isAdmin ? _userTypeaheadSearch(value) : _usersTypeaheadSearch(value);
  }

  // making this explicit allows us to only react to input events,
  // rather than programmatic changes like selecting a user
  let trigger = writable(0);
  const _typeaheadResults = deriveAsync(
    trigger,
    typeaheadSearch,
    [],
    debounceMs);


  const dispatch = createEventDispatcher<{
      selectedUserChange: UserTypeaheadResult | null;
  }>();

  let selectedUser = writable<UserTypeaheadResult | null>(null);

  function selectUser(user: UserTypeaheadResult): void {
    $selectedUser = user;
    dispatch('selectedUserChange', $selectedUser);
    value = getInputValue(user);
  }


  function formatResult(user: UserTypeaheadResult): string {
    const extra = 'username' in user && user.username && 'email' in user && user.email ? ` (${user.username}, ${user.email})`
                : 'username' in user && user.username ? ` (${user.username})`
                : 'email' in user && user.email ? ` (${user.email})`
                : '';
    return `${user.name}${extra}`;
  }

  function getInputValue(user: UserTypeaheadResult): string {
    if ('email' in user && user.email) return user.email;
    if ('username' in user && user.username) return user.username;
    if ('name' in user && user.name) return user.name;
    return '';
  }

  let highlightIdx: number | undefined = $state(undefined);
  let typeaheadOpen = $state(false);
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

  function onOverlayOpen(open: boolean): void {
    typeaheadOpen = open;
    if (!typeaheadOpen) {
      typeaheadResults = []; // prevent old results showing when opening next time
    }
  }
  let typeaheadResults = $derived($_typeaheadResults);
  let filteredResults = $derived(typeaheadResults.filter(user => !exclude.includes(user.id)));
  run(() => {
    if ($selectedUser && value !== getInputValue($selectedUser)) {
      $selectedUser = null;
      dispatch('selectedUserChange', $selectedUser);
    }
  });
  run(() => {
    if (typeaheadOpen) {
      highlightIdx ??= filteredResults.length ? 0 : undefined;
    } else {
      highlightIdx = undefined;
    }
  });
</script>

<FormField {id} {label} {error} {autofocus}>
  <div use:overlay={{ closeClickSelector: '.menu li', onOverlayOpen}}>
    <PlainInput
      style="w-full"
      bind:value
      {id}
      type="text"
      autocomplete="off"
      {autofocus}
      {keydownHandler}
      on:input={() => $trigger++}
    />
    <div class="overlay-content">
      <ul class="menu p-0">
      {#each filteredResults as user, idx}
        <li class={(highlightIdx == idx) ? 'p-0 bg-primary text-white' : 'p-0'}><button class="whitespace-nowrap" onclick={() => {
          setTimeout(() => selectUser(user));
        }}>{formatResult(user)}</button></li>
      {/each}
      </ul>
    </div>
  </div>
</FormField>
