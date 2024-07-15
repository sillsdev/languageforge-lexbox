<script lang="ts">
  import type { FlExWsId } from '$lib/gql/types';
  import BadgeList from '../Badges/BadgeList.svelte';
  import WritingSystemBadge from './WritingSystemBadge.svelte';

  export let writingSystems: FlExWsId[] = [];
</script>

<BadgeList>
  {#each  writingSystems as ws}
    <WritingSystemBadge tag={ws.tag} isActive={ws.isActive} isDefault={ws.isDefault} />
  {/each}
</BadgeList>
