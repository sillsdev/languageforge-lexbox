<script lang="ts" module>
  import { cubicOut } from 'svelte/easing';

  const [send, receive] = crossfade({
    duration: 500,
    easing: cubicOut,
  });

  const instances: { [key: string]: boolean | undefined } = $state({});
</script>

<script lang="ts">
  import { crossfade } from 'svelte/transition';
  import { Button } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';

  $effect(() => {
    instances[id] = active;
    return () => {
      delete instances[id];
    };
  });

  const {
    onclick,
    active,
    shortForm = false,
  }: {
    onclick: () => void;
    shortForm?: boolean;
    active?: boolean;
  } = $props();

  const id = crypto.randomUUID();
  const isActive = $derived(
    // explicitly active
    instances[id] === true ||
    // implicitly active (not explicitly inactive and no other instance is active)
    instances[id] === undefined && !Object.values(instances).some(_active => _active));
</script>

{#if isActive}
  <div in:receive={{ key: 'new-entry-button' }} out:send={{ key: 'new-entry-button' }}>
    <Button variant="default" size="extended-fab" class="font-semibold" icon="i-mdi-plus-thick" {onclick}>
      {#if shortForm}
        <span>{$t`New`}</span>
      {:else}
        <span>{$t`New Entry`}</span>
      {/if}
    </Button>
  </div>
{/if}
