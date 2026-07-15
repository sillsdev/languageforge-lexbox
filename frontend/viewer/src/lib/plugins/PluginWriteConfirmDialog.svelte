<script lang="ts" module>
  /** 'once' applies this write only; 'always' applies it AND stops asking for this plugin. */
  export type WriteConfirmResult = 'deny' | 'once' | 'always';
</script>

<script lang="ts">
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import type {PluginWriteOperation} from './plugin-api-types';

  interface Props {
    pluginName: string;
    operation: PluginWriteOperation | undefined;
    onResult: (result: WriteConfirmResult) => void;
  }

  const {pluginName, operation, onResult}: Props = $props();
</script>

<AlertDialog.Root open={!!operation} onOpenChange={(open) => { if (!open) onResult('deny'); }}>
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
      <div class="max-h-[50vh] overflow-y-auto rounded-md border bg-muted/50 p-3 text-sm space-y-1">
        {#each operation.summary as line, i (i)}
          <div class="break-words" dir="auto">{line}</div>
        {:else}
          <div class="text-muted-foreground">{$t`No visible changes`}</div>
        {/each}
      </div>
    {/if}
    <AlertDialog.Footer class="flex-wrap gap-2">
      <AlertDialog.Cancel onclick={() => onResult('deny')}>{$t`Don't allow`}</AlertDialog.Cancel>
      <!-- Applies this change too — trusting a plugin shouldn't force the user to re-trigger it. -->
      <Button variant="outline" onclick={() => onResult('always')}>
        {$t`Always allow for this plugin`}
      </Button>
      <AlertDialog.Action onclick={() => onResult('once')}>
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
