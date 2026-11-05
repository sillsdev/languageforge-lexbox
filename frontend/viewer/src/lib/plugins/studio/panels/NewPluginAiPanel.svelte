<script lang="ts">
  import * as Collapsible from '$lib/components/ui/collapsible';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Switch} from '$lib/components/ui/switch';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectContext} from '$project/project-context.svelte';
  import {buildPluginPrompt, pluginTaskSection} from '../../plugin-prompt';
  import {useStudio} from '../plugin-studio.svelte';
  import {downloadPrompt} from '../studio-files';
  import PasteBox from './PasteBox.svelte';

  const studio = useStudio();
  const projectContext = useProjectContext();

  let basePrompt = $state('');
  let promptGeneration = 0;
  $effect(() => {
    const current = {...studio.promptOptions};
    const token = ++promptGeneration;
    void buildPluginPrompt(projectContext.api, {
      projectName: projectContext.projectName,
      projectCode: projectContext.projectCode,
    }, current).then(result => { if (token === promptGeneration) basePrompt = result; });
  });
  const newPluginPrompt = $derived(basePrompt ? `${basePrompt}\n\n${pluginTaskSection(studio.aiDescription)}` : '');
</script>

<ol class="space-y-5">
  <li class="space-y-2">
    <div class="font-medium">{$t`1. Describe your plugin`}</div>
    <Textarea
      bind:value={studio.aiDescription}
      aria-label={$t`Describe your plugin`}
      class="min-h-24"
      placeholder={$t`e.g. A flashcard game that quizzes me on entries missing an English gloss and lets me fill it in when I remember the word.`}
    />
    <Collapsible.Root bind:open={studio.showPromptOptions}>
      <Collapsible.Trigger class="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <Icon icon="i-mdi-chevron-right" class="transition-transform {studio.showPromptOptions ? 'rotate-90' : ''}" />
        {$t`Prompt options`}
      </Collapsible.Trigger>
      <Collapsible.Content class="space-y-3 pt-2 text-sm">
        <p class="text-xs text-muted-foreground">
          {$t`These guide how the AI writes the plugin. They don't restrict what the finished plugin can do.`}
        </p>
        <Switch label={$t`Tailor to this project`} bind:checked={studio.promptOptions.projectSpecific} />
        <Switch label={$t`Works on phones`} bind:checked={studio.promptOptions.mobile} />
        <Switch label={$t`Can change the dictionary`} bind:checked={studio.promptOptions.allowEdits} />
        <Switch label={$t`Can use the internet`} bind:checked={studio.promptOptions.internet} />
        <Switch label={$t`Handle content with cultural care`} bind:checked={studio.promptOptions.culturalSensitivity} />
      </Collapsible.Content>
    </Collapsible.Root>
  </li>
  <li class="space-y-2">
    <div class="font-medium">{$t`2. Have an AI write it`}</div>
    <p class="text-sm text-muted-foreground">
      {$t`Download the prompt and attach the file in any AI chat (e.g. Claude, ChatGPT or Gemini). It already explains plugins and this project.`}
    </p>
    <Button icon="i-mdi-download" disabled={!newPluginPrompt} loading={!newPluginPrompt} onclick={() => downloadPrompt(newPluginPrompt, studio.draft.name)}>
      {$t`Download prompt`}
    </Button>
  </li>
  <li class="space-y-2">
    <div class="font-medium">{$t`3. Try it here`}</div>
    <PasteBox label={$t`Paste the AI's reply`} />
  </li>
</ol>
