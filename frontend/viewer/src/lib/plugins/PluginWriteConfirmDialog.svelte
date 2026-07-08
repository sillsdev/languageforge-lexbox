<script lang="ts">
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import type {PluginWriteOperation} from './plugin-api-types';

  interface Props {
    pluginName: string;
    operation: PluginWriteOperation | undefined;
    onResult: (approved: boolean) => void;
  }

  const {pluginName, operation, onResult}: Props = $props();
</script>

<AlertDialog.Root open={!!operation} onOpenChange={(open) => { if (!open) onResult(false); }}>
  <AlertDialog.Content class="max-w-lg">
    <AlertDialog.Header>
      <AlertDialog.Title class="flex items-center gap-2">
        <Icon icon="i-mdi-puzzle" class="text-primary" />
        {#if operation?.kind === 'createEntry'}
          {$t`Plugin wants to add an entry`}
        {:else if operation?.kind === 'batch'}
          {$t`Plugin wants to make ${operation.count} changes`}
        {:else}
          {$t`Plugin wants to change an entry`}
        {/if}
      </AlertDialog.Title>
      <AlertDialog.Description>
        {$t`The plugin “${pluginName}” is asking to make this change to your dictionary:`}
      </AlertDialog.Description>
    </AlertDialog.Header>
    {#if operation}
      <div class="max-h-64 overflow-y-auto rounded-md border bg-muted/50 p-3 text-sm space-y-1">
        {#each operation.summary as line, i (i)}
          <div class="break-words">{line}</div>
        {:else}
          <div class="text-muted-foreground">{$t`No visible changes`}</div>
        {/each}
      </div>
    {/if}
    <AlertDialog.Footer>
      <AlertDialog.Cancel onclick={() => onResult(false)}>{$t`Don't allow`}</AlertDialog.Cancel>
      <AlertDialog.Action onclick={() => onResult(true)}>
        {#if operation?.kind === 'createEntry'}
          {$t`Add entry`}
        {:else if operation?.kind === 'batch'}
          {$t`Apply changes`}
        {:else}
          {$t`Apply change`}
        {/if}
      </AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
