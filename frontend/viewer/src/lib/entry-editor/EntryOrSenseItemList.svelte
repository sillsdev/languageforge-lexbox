<script lang="ts">
  import OrderedItemList from './OrderedItemList.svelte';

  import { Icon, MenuItem } from 'svelte-ux';
  import { mdiBookOutline } from '@mdi/js';
  import { Link } from 'svelte-routing';

  type T = $$Generic;

  export let value: T[];
  export let readonly: boolean;
  export let getEntryId: (item: T) => string;
  export let getHeadword: (item: T) => string | undefined;
</script>

<OrderedItemList bind:value {readonly} on:change
  getDisplayName={getHeadword}
  getGotoLink={entry => `?entryId=${getEntryId(entry)}&search=${getHeadword?.(entry)?.replace(/\d?$/, '')}`}>
  <svelte:fragment slot="menuItems" let:item={entry}>
    <MenuItem class="gap-2">
      <Link to="?entryId={getEntryId(entry)}&search={getHeadword?.(entry)?.replace(/\d?$/, '')}">
        Go to {getHeadword?.(entry) || '–'}
        <Icon data={mdiBookOutline} />
      </Link>
    </MenuItem>
  </svelte:fragment>
  <slot name="actions" slot="actions" />
</OrderedItemList>
