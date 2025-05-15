<script lang="ts">
  import { Badge } from '../Badges';
  import type { BadgeVariant } from '../Badges/Badge.svelte';

  interface Props {
    tag: string;
    isActive: boolean;
    isDefault: boolean;
  }

  const { tag, isActive, isDefault }: Props = $props();

  let variant: BadgeVariant = $derived(
    isActive ? (isDefault ? 'badge-primary' : 'badge-info') : 'badge-neutral'
  );
</script>

<Badge {variant}>
  {tag}
</Badge>
