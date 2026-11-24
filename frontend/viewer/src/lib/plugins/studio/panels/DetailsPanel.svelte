<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Input} from '$lib/components/ui/input';
  import {Label} from '$lib/components/ui/label';
  import {t} from 'svelte-i18n-lingui';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useStudio} from '../plugin-studio.svelte';

  const studio = useStudio();
  const draft = studio.draft;
  const dialogsService = useDialogsService();

  async function removePlugin() {
    if (!studio.savedPlugin) return;
    const shouldDelete = await dialogsService.promptDelete($t`Plugin`, studio.savedPlugin.name, {
      isDangerous: true,
      details: $t`This removes the plugin for everyone on this project.`,
    });
    if (shouldDelete) await studio.removePlugin();
  }
</script>

<div class="space-y-4">
  <div class="space-y-2">
    <Label for="plugin-studio-name">{$t`Name`}</Label>
    <Input id="plugin-studio-name" bind:value={draft.name} oninput={() => studio.nameFromHtml = false} placeholder={$t`e.g. Dictionary stats`} />
    {#if studio.nameFromHtml}
      <p class="text-xs text-muted-foreground">{$t`Filled in from the plugin's title. Change it if you like.`}</p>
    {/if}
  </div>
  <div class="space-y-2">
    <Label for="plugin-studio-description">{$t`Description`}</Label>
    <Input id="plugin-studio-description" bind:value={draft.description} placeholder={$t`e.g. Dashboard of dictionary statistics`} />
    <p class="text-xs text-muted-foreground">{$t`A short summary shown on the plugin's card.`}</p>
  </div>
  {#if studio.mode === 'edit'}
    <Button variant="outline" class="text-destructive" icon="i-mdi-trash-can-outline" onclick={() => void removePlugin()}>
      {$t`Remove from project`}
    </Button>
  {/if}
</div>
