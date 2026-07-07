<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectContext} from '$project/project-context.svelte';
  import {buildPluginPrompt} from './plugin-prompt';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  interface Props {
    open: boolean;
  }

  let {open = $bindable()}: Props = $props();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'plugin-ai-prompt-dialog'});

  const projectContext = useProjectContext();
  let prompt = $state('');
  let copied = $state(false);

  $effect(() => {
    if (!open) return;
    copied = false;
    void buildPluginPrompt(projectContext.api, {
      projectName: projectContext.projectName,
      projectCode: projectContext.projectCode,
    }).then(result => prompt = result);
  });

  async function copy() {
    await navigator.clipboard.writeText(prompt);
    copied = true;
    AppNotification.display($t`Prompt copied to clipboard`, {timeout: 'short', type: 'success'});
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-3xl">
    <Dialog.Header>
      <Dialog.Title>{$t`Create a plugin with AI`}</Dialog.Title>
      <Dialog.Description>
        {$t`No coding needed — an AI assistant can write the plugin for you.`}
      </Dialog.Description>
    </Dialog.Header>

    <ol class="list-decimal ms-5 space-y-1 text-sm">
      <li>{$t`Copy the prompt below. It already contains everything the AI needs to know about plugins and this project.`}</li>
      <li>{$t`Paste it into an AI assistant (e.g. Claude or ChatGPT) and replace the last paragraph with a description of the plugin you want.`}</li>
      <li>{$t`Copy the HTML file the AI produces, then add it here via “New plugin”.`}</li>
    </ol>

    {#if prompt}
      <Textarea
        readonly
        value={prompt}
        spellcheck="false"
        class="font-mono text-xs min-h-48 max-h-[40vh]"
        onclick={(event) => (event.currentTarget as HTMLTextAreaElement).select()}
      />
    {:else}
      <div class="flex items-center justify-center gap-2 min-h-48 text-muted-foreground">
        <Icon icon="i-mdi-loading" class="animate-spin" />
        {$t`Gathering project information…`}
      </div>
    {/if}

    <Dialog.Footer>
      <Button variant="outline" onclick={() => open = false}>{$t`Close`}</Button>
      <Button disabled={!prompt} icon={copied ? 'i-mdi-check' : 'i-mdi-content-copy'} onclick={() => void copy()}>
        {#if copied}
          {$t`Copied`}
        {:else}
          {$t`Copy prompt`}
        {/if}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
