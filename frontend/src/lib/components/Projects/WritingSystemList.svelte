<script lang="ts">
  import type { FlExWsId } from '$lib/gql/types';
  import BadgeList from '../Badges/BadgeList.svelte';
  import WritingSystemBadge from './WritingSystemBadge.svelte';

  interface Props {
    writingSystems?: FlExWsId[];
  }

  const { writingSystems = [] }: Props = $props();
</script>

<div class="w-full">
  {#if writingSystems.length > 0}
    <BadgeList>
      {#each writingSystems as ws (ws.tag)}
        <WritingSystemBadge tag={ws.tag} isActive={ws.isActive} isDefault={ws.isDefault} />
      {/each}
    </BadgeList>
  {/if}
</div>
