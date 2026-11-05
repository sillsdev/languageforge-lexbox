<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Switch} from '$lib/components/ui/switch';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectContext} from '$project/project-context.svelte';
  import {buildPluginPrompt, pluginTaskSection, type PluginPromptOptions} from './plugin-prompt';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  interface Props {
    open: boolean;
  }

  let {open = $bindable()}: Props = $props();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'plugin-ai-prompt-dialog'});

  const projectContext = useProjectContext();
  let description = $state('');
  let basePrompt = $state('');
  let showPrompt = $state(false);
  let copied = $state(false);

  // The description is spliced in cheaply; only the options rebuild the (query-backed) body.
  const fullPrompt = $derived(basePrompt ? `${basePrompt}\n\n${pluginTaskSection(description)}` : '');
  $effect(() => {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    fullPrompt;
    copied = false;
  });

  // These only steer the generated prompt; they don't restrict what a finished plugin can do.
  // Every switch reads "on = plugin gains this capability/trait", so the mixed default positions
  // are intentional: on for the permissive defaults, off for the two with a privacy/values cost.
  const options = $state<PluginPromptOptions>({
    projectSpecific: true,
    mobile: true,
    allowEdits: true,
    internet: false,
    culturalSensitivity: false,
  });

  let generation = 0;
  $effect(() => {
    if (!open) return;
    const current = {...options};
    const token = ++generation;
    void buildPluginPrompt(projectContext.api, {
      projectName: projectContext.projectName,
      projectCode: projectContext.projectCode,
    }, current).then(result => { if (token === generation) basePrompt = result; });
  });

  async function copy() {
    await navigator.clipboard.writeText(fullPrompt);
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
      <li>{$t`Describe the plugin you want below.`}</li>
      <li>{$t`Copy the prompt and paste it into an AI assistant (e.g. Claude or ChatGPT). It already contains everything the AI needs to know about plugins and this project.`}</li>
      <li>{$t`Copy the HTML file the AI produces, then add it here via “New plugin”.`}</li>
    </ol>

    <div class="space-y-1.5">
      <label for="plugin-description" class="text-sm font-medium">{$t`Describe the plugin you want`}</label>
      <Textarea
        id="plugin-description"
        bind:value={description}
        spellcheck="true"
        class="min-h-24"
        placeholder={$t`e.g. A flashcard game that quizzes me on entries missing an English gloss and lets me fill it in when I remember the word.`}
      />
    </div>

    <div class="rounded-md border p-3 space-y-2.5">
      <div class="text-xs text-muted-foreground">
        {$t`These options tailor the prompt for the AI — they guide how it writes the plugin, they don't restrict what the finished plugin can do.`}
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-3 text-sm">
        <div class="space-y-1">
          <Switch label={$t`Tailor to this project`} bind:checked={options.projectSpecific} />
          <p class="text-xs text-muted-foreground ms-12">
            {$t`Lets the AI use this project's languages and categories. Turn off to build a plugin that works in any project.`}
          </p>
        </div>
        <div class="space-y-1">
          <Switch label={$t`Works on phones`} bind:checked={options.mobile} />
          <p class="text-xs text-muted-foreground ms-12">
            {$t`Makes the layout fit small phone screens, not just desktop.`}
          </p>
        </div>
        <div class="space-y-1">
          <Switch label={$t`Can change the dictionary`} bind:checked={options.allowEdits} />
          <p class="text-xs text-muted-foreground ms-12">
            {$t`The plugin may suggest edits — you still approve each change. Turn off for a view-only plugin.`}
          </p>
        </div>
        <div class="space-y-1">
          <Switch label={$t`Can use the internet`} bind:checked={options.internet} />
          <p class="text-xs text-muted-foreground ms-12">
            {$t`Lets it load things from the web. Off keeps the plugin fully offline.`}
          </p>
        </div>
      </div>
      <div class="border-t pt-2.5 space-y-1">
        <Switch label={$t`Handle content with cultural care`} bind:checked={options.culturalSensitivity} />
        <p class="text-xs text-muted-foreground ms-12">
          {$t`Some words may be sacred or sensitive, and a plugin can't tell which — so it treats all the language data respectfully and won't trivialize or gamify it.`}
        </p>
      </div>
    </div>

    <Collapsible.Root bind:open={showPrompt}>
      <Collapsible.Trigger class="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <Icon icon="i-mdi-chevron-right" class="transition-transform {showPrompt ? 'rotate-90' : ''}" />
        {$t`Preview the full prompt`}
      </Collapsible.Trigger>
      <Collapsible.Content>
        {#if fullPrompt}
          <Textarea
            readonly
            value={fullPrompt}
            spellcheck="false"
            class="mt-2 font-mono text-xs min-h-48 max-h-[40vh]"
            onclick={(event) => (event.currentTarget as HTMLTextAreaElement).select()}
          />
        {:else}
          <div class="mt-2 flex items-center justify-center gap-2 min-h-24 text-muted-foreground">
            <Icon icon="i-mdi-loading" class="animate-spin" />
            {$t`Gathering project information…`}
          </div>
        {/if}
      </Collapsible.Content>
    </Collapsible.Root>

    <Dialog.Footer>
      <Button variant="outline" onclick={() => open = false}>{$t`Close`}</Button>
      <Button disabled={!fullPrompt} icon={copied ? 'i-mdi-check' : 'i-mdi-content-copy'} onclick={() => void copy()}>
        {#if copied}
          {$t`Copied`}
        {:else}
          {$t`Copy prompt`}
        {/if}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
