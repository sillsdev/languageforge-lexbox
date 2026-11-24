<script lang="ts">
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio} from './plugin-studio.svelte';

  const studio = useStudio();
</script>

<AlertDialog.Root open={!!studio.leavePrompt} onOpenChange={(open) => { if (!open) studio.answerLeavePrompt(false); }}>
  <AlertDialog.Content>
    <AlertDialog.Header>
      <AlertDialog.Title>{$t`Leave without saving?`}</AlertDialog.Title>
      <AlertDialog.Description>
        {#if studio.mode === 'edit'}
          {$t`Your changes to this plugin will be lost.`}
        {:else}
          {$t`This plugin hasn't been added to the project, so it will be lost.`}
        {/if}
      </AlertDialog.Description>
    </AlertDialog.Header>
    <AlertDialog.Footer>
      <AlertDialog.Cancel onclick={() => studio.answerLeavePrompt(false)}>{$t`Keep editing`}</AlertDialog.Cancel>
      <AlertDialog.Action onclick={() => studio.answerLeavePrompt(true)}>{$t`Leave`}</AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
