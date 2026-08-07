<script module lang="ts">
  /** Keyboard modifier for app shortcuts. Extend this union as more modifiers are supported. */
  export type Modifier = 'primary';
</script>

<script lang="ts">
  import {getOpenDialogCount} from '$lib/components/ui/dialog-shared/dialog-shared-root.svelte';
  import {hasPrimaryModifier} from '$lib/utils/platform';

  type Props = {
    key: string;
    modifier?: Modifier;
    disabled?: boolean;
    allowWhenDialogOpen?: boolean;
    onHotkey: () => void;
  };

  let {key, modifier = 'primary', disabled = false, allowWhenDialogOpen = false, onHotkey}: Props = $props();

  function matchesModifier(e: KeyboardEvent, mod: Modifier): boolean {
    switch (mod) {
      case 'primary':
        return hasPrimaryModifier(e);
    }
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key.toLowerCase() !== key.toLowerCase() || !matchesModifier(e, modifier)) return;
    if (disabled) return;
    if (!allowWhenDialogOpen && getOpenDialogCount() > 0) return;
    e.preventDefault();
    onHotkey();
  }
</script>

<svelte:window onkeydown={handleKeydown} />
