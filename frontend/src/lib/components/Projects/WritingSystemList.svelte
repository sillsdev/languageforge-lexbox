<script lang="ts">
  import BadgeList from '../Badges/BadgeList.svelte';
  import WritingSystemBadge from './WritingSystemBadge.svelte';

  export let writingSystems = [];
</script>

<BadgeList>
  {#each  writingSystems as ws}
    <WritingSystemBadge tag={ws.tag} isActive={ws.isActive} isDefault={ws.isDefault} />
  {/each}
</BadgeList>
