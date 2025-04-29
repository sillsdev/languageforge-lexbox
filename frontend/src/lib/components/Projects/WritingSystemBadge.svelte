<script lang="ts">
  import { run } from 'svelte/legacy';

  import { Badge } from '../Badges';
  import type { BadgeVariant } from '../Badges/Badge.svelte';

  interface Props {
    tag: string;
    isActive: boolean;
    isDefault: boolean;
  }

  let { tag, isActive, isDefault }: Props = $props();

  let variant: BadgeVariant = $state('badge-info');
  run(() => {
    variant = isActive ? (isDefault ? 'badge-primary' : 'badge-info') : 'badge-neutral';
  });

</script>

<Badge {variant}>
  {tag}
</Badge>
