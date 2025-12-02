<script lang="ts">
  import {FormField, PlainInput, randomFormId} from '$lib/forms';
  import {_userTypeaheadSearch, _usersTypeaheadSearch, type SingleUserTypeaheadResult, type SingleUserICanSeeTypeaheadResult} from '$lib/gql/typeahead-queries';
  import {overlay} from '$lib/overlay';
  import {resource} from 'runed';

  type UserTypeaheadResult = SingleUserTypeaheadResult | SingleUserICanSeeTypeaheadResult;
  let inputComponent: PlainInput | undefined = $state();

  interface Props {
    label: string;
    error?: string | string[];
    id?: string;
    autofocus?: true;
    value: string;
    debounceMs?: number;
    isAdmin?: boolean;
    exclude?: string[];
    onSelectedUserChange?: (selection: UserTypeaheadResult | null) => void;
  }

  let {
    label,
    error,
    id = randomFormId(),
    autofocus,
    value = $bindable(),
    debounceMs = 200,
    isAdmin = false,
    exclude = [],
    onSelectedUserChange,
  }: Props = $props();

  let selectedValue: string | undefined = $state();

  function typeaheadSearch(value: string): Promise<UserTypeaheadResult[]> {
    return isAdmin ? _userTypeaheadSearch(value) : _usersTypeaheadSearch(value);
  }

  // making this explicit allows us to only react to input events,
  // rather than programmatic changes like selecting a user
  let trigger = $state('');

  const _typeaheadResults = resource(() => trigger,
    (value) => typeaheadSearch(value),
    {initialValue: [], debounce: (() => debounceMs)()},
  );

  let selectedUser = $state<UserTypeaheadResult | null>(null);

  function selectUser(user: UserTypeaheadResult): void {
    selectedUser = user;
    onSelectedUserChange?.(selectedUser);
    selectedValue = getInputValue(user);
    value = selectedValue;
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
  function keydownHandler(event: KeyboardEvent): void {
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
  let typeaheadResults = $derived(_typeaheadResults.current);
  let filteredResults = $derived(typeaheadResults.filter((user) => !exclude.includes(user.id)));
  // TODO: Can this be simplified by making the "value !== selectedValue" part into a $derived?
  // Then we'd be able to just do `$effect(() => { if (changedSelection) dispatch(...)})`
  // And, of course, change the dispatch into calling a prop function
  $effect(() => {
    if (selectedUser && value !== selectedValue) {
      selectedUser = null;
      selectedValue = undefined;
      onSelectedUserChange?.(null);
    }
  });
  $effect(() => {
    if (typeaheadOpen) {
      highlightIdx ??= filteredResults.length ? 0 : undefined;
    } else {
      highlightIdx = undefined;
    }
  });
</script>

<FormField {id} {label} {error} {autofocus}>
  <div use:overlay={{ closeClickSelector: '.menu li', onOverlayOpen }}>
    <PlainInput
      bind:this={inputComponent}
      style="w-full"
      bind:value
      {id}
      type="text"
      autocomplete="off"
      {autofocus}
      {keydownHandler}
      onInput={(value) => trigger = value ?? ''}
    />
    <div class="overlay-content">
      <ul class="menu p-0">
      {#each filteredResults as user, idx (user.id)}
        <li class={(highlightIdx == idx) ? 'p-0 bg-primary text-white' : 'p-0'}><button class="whitespace-nowrap" onclick={() => {
          setTimeout(() => selectUser(user));
        }}>{formatResult(user)}</button></li>
      {/each}
      </ul>
    </div>
  </div>
</FormField>
