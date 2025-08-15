<script lang="ts" module>
  import {crossfade} from 'svelte/transition';
  import { cubicOut } from 'svelte/easing';

  const [send, receive] = crossfade({
    duration: 500,
    easing: cubicOut,
  });

  const instances: { [key: string]: boolean | undefined } = $state({});
</script>

<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import {useFeatures} from '$lib/services/feature-service';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';

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

  const currentView = useCurrentView();
  const features = useFeatures();
  const id = $props.id();
  const isActive = $derived(
    // explicitly active
    instances[id] === true ||
    // implicitly active (not explicitly inactive and no other instance is active)
    instances[id] === undefined && !Object.values(instances).some(_active => _active));
</script>

{#if isActive && features.write}
  <div class="relative z-[1]" in:receive={{ key: 'new-entry-button' }} out:send={{ key: 'new-entry-button' }}>
    <Button variant="default" size="extended-fab" class="font-semibold" icon="i-mdi-plus-thick" {onclick}>
      {#if shortForm}
        <span>{$t`New`}</span>
      {:else}
        <span>{pt($t`New Entry`, $t`New Word`, $currentView)}</span>
      {/if}
    </Button>
  </div>
{/if}
