<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio} from '../plugin-studio.svelte';

  interface Props {
    label: string;
  }

  const {label}: Props = $props();
  const studio = useStudio();
</script>

<div class="space-y-2">
  <Label for="plugin-studio-paste">{label}</Label>
  <Textarea
    id="plugin-studio-paste"
    bind:value={studio.pasteText}
    spellcheck="false"
    placeholder={$t`Paste the AI's reply or the plugin's HTML here`}
    class="font-mono text-xs min-h-28 max-h-[40vh]"
  />
  {#if studio.pasteError}
    <p class="text-sm text-destructive">
      {$t`This doesn't look like a complete plugin. It should be one HTML file that starts with <!DOCTYPE html> and ends with </html>.`}
    </p>
  {/if}
  {#if studio.pasteNote}
    <p class="rounded-md bg-muted px-3 py-2 text-sm">
      {$t`Follow-up prompt downloaded. Attach it in the same AI chat, then paste its reply here.`}
    </p>
  {/if}
  <Button variant="outline" size="sm" icon="i-mdi-check" disabled={!studio.pasteText.trim()} onclick={() => void studio.applyPaste()}>
    {$t`Use this version`}
  </Button>
</div>
