<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {badgeVariants} from '$lib/components/ui/badge';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$project/data';
  import {Button, buttonVariants} from '$lib/components/ui/button';

  type Props = {
    /** Selected writing system id to sort/display by. Undefined = the default vernacular. */
    value?: string;
  };

  let {value = $bindable()}: Props = $props();

  const writingSystemService = useWritingSystemService();
  // Vernacular writing systems, excluding audio — the ones a headword can sort by.
  const writingSystems = $derived(writingSystemService.vernacularNoAudio);
  const selectedWsId = $derived(value ?? writingSystemService.defaultVernacular?.wsId);
  const selectedWs = $derived(writingSystems.find(ws => ws.wsId === selectedWsId));
</script>

<!-- With only one option there's nothing to choose, so it behaves like it always has: no pill. -->
{#if writingSystems.length > 1}
  <ResponsiveMenu.Root>
    <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), badgeVariants({ variant: 'secondary' }), 'border-none h-7')}>
      {#snippet child({props})}
        <Button {...props}
          data-testid="sort-ws-trigger"
          title={$t`Sort writing system`}
          icon="i-mdi-translate"
          iconProps={{ class: 'size-4' }}>
          {selectedWs?.abbreviation || selectedWs?.name || ''}
        </Button>
      {/snippet}
    </ResponsiveMenu.Trigger>
    <ResponsiveMenu.Content align="start">
      {#each writingSystems as ws (ws.wsId)}
        <ResponsiveMenu.Item
          onSelect={() => value = ws.wsId}
          class={cn(selectedWsId === ws.wsId && 'bg-muted')}
          >
          {ws.name}
          <span class="text-muted-foreground ml-auto text-xs">{ws.abbreviation || ws.wsId}</span>
        </ResponsiveMenu.Item>
      {/each}
    </ResponsiveMenu.Content>
  </ResponsiveMenu.Root>
{/if}
