<script lang="ts">
  import {Badge} from '$lib/components/ui/badge';
  import type {Primitive} from 'type-fest';
  import {t} from 'svelte-i18n-lingui';

  const {items = [], placeholder}: {items?: ReadonlyArray<{id: Primitive; label: string}>; placeholder?: string} =
    $props();
</script>

<div class="flex flex-wrap justify-start gap-2 overflow-hidden">
  {#each items as item (item.id)}
    <Badge>
      {item.label || $t`Untitled`}
    </Badge>
  {:else}
    <span class="text-muted-foreground x-ellipsis">
      {placeholder ?? $t`None`}
      <!-- ensures that:
        1) baseline alignment works for consumers of this component
        2) list height doesn't shrink when empty
        -->
      <Badge class="max-w-0 invisible">&nbsp;</Badge>
    </span>
  {/each}
</div>
