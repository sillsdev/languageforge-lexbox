<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio} from '../plugin-studio.svelte';
  import PasteBox from './PasteBox.svelte';
  import VersionReview from './VersionReview.svelte';

  const studio = useStudio();
</script>

<div class="space-y-4">
  {#if studio.review}
    <VersionReview review={studio.review} />
  {/if}

  <div class="space-y-2">
    <Label for="plugin-studio-change">{studio.mode === 'example' && !studio.added ? $t`Change it before adding` : $t`Describe a change`}</Label>
    <Textarea
      id="plugin-studio-change"
      bind:value={studio.changeText}
      class="min-h-20"
      placeholder={$t`e.g. Also show how many entries have a recording.`}
    />
    <Button icon="i-mdi-download" disabled={!studio.draft.complete} loading={studio.buildingChangePrompt} onclick={() => void studio.downloadChangePrompt(studio.changeText)}>
      {$t`Download prompt`}
    </Button>
    <p class="text-xs text-muted-foreground">
      {$t`The prompt includes the API guide, this project's setup and this plugin's current code. Attach the file in any AI chat.`}
    </p>
  </div>

  <PasteBox label={$t`Paste the AI's reply`} />

  {#if studio.draft.previousVersions.length}
    <Button variant="ghost" size="sm" icon="i-mdi-undo" onclick={() => studio.undoVersion()}>
      {$t`Go back to the previous version`}
    </Button>
  {/if}
</div>
