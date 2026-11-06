<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {useStudio} from '../plugin-studio.svelte';
  import {downloadPluginHtml} from '../studio-files';
  import PasteBox from './PasteBox.svelte';

  const studio = useStudio();
  const draft = studio.draft;
  let fileInput = $state<HTMLInputElement>();

  async function uploadFile(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    await studio.useHtml(await file.text());
  }
</script>

<div class="flex h-full flex-col gap-3">
  <div class="flex flex-wrap gap-2">
    <Button variant="outline" size="sm" icon="i-mdi-upload" onclick={() => fileInput?.click()}>{$t`Upload file`}</Button>
    {#if draft.html}
      <Button variant="outline" size="sm" icon="i-mdi-download" onclick={() => downloadPluginHtml(draft.html, draft.name)}>{$t`Download`}</Button>
      <span class="self-center text-xs text-muted-foreground">{$t`${studio.sizeKb} KB`}</span>
    {/if}
    <input bind:this={fileInput} type="file" accept=".html,.htm" class="hidden" onchange={uploadFile} />
  </div>
  {#if studio.mode === 'new' && !draft.html}
    <p class="text-sm text-muted-foreground">{$t`Already have a plugin file? It runs in the preview straight away, before anyone else can see it.`}</p>
    <PasteBox label={$t`Paste HTML`} />
  {:else}
    <Textarea
      readonly
      value={draft.html}
      aria-label={$t`Plugin HTML`}
      spellcheck="false"
      class="font-mono text-xs grow min-h-64"
    />
  {/if}
</div>
