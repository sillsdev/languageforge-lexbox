<script lang="ts">
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio} from './plugin-studio.svelte';

  const studio = useStudio();
</script>

<AlertDialog.Root open={!!studio.internetPrompt} onOpenChange={(open) => { if (!open) studio.answerInternetPrompt('cancel'); }}>
  <AlertDialog.Content data-testid="plugin-internet-prompt">
    <AlertDialog.Header>
      <AlertDialog.Title>{$t`Let this version use the internet?`}</AlertDialog.Title>
      <AlertDialog.Description class="space-y-2">
        <span class="block">
          {studio.draft.html
            ? $t`The version before it worked offline. Online, a plugin can send this project's data to any website.`
            : $t`Online, a plugin can send this project's data to any website.`}
        </span>
        <span class="block">{$t`Only allow it if the plugin needs to go online, for example to look up words or download pictures.`}</span>
      </AlertDialog.Description>
    </AlertDialog.Header>
    <AlertDialog.Footer class="gap-2">
      <AlertDialog.Cancel onclick={() => studio.answerInternetPrompt('cancel')}>{$t`Don't run it`}</AlertDialog.Cancel>
      <Button variant="outline" loading={studio.buildingChangePrompt} onclick={() => void studio.askToRemoveInternetFirst()}>
        {$t`Ask the AI to remove it`}
      </Button>
      <AlertDialog.Action onclick={() => studio.answerInternetPrompt('allow')}>{$t`Allow and run it`}</AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
