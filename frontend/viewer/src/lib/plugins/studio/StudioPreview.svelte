<script lang="ts">
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {type IEntry, SortField} from '$lib/dotnet-types';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import EntryOrSensePicker from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import {createPluginDocUrl} from '../plugin-srcdoc';
  import PluginSandbox from '../PluginSandbox.svelte';
  import PluginConsentCard from '../PluginConsentCard.svelte';
  import {useStudio} from './plugin-studio.svelte';

  interface Props {
    title: string;
  }

  const {title}: Props = $props();
  const studio = useStudio();
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();

  let reloadToken = $state(0);
  let phoneWidth = $state(false);

  let docUrl = $state<string>();
  $effect(() => {
    const doc = studio.previewDoc;
    if (!doc) {
      docUrl = undefined;
      return;
    }
    const url = createPluginDocUrl(doc.html, doc.manifest.permissions);
    docUrl = url;
    return () => URL.revokeObjectURL(url);
  });

  // Entry-menu plugins expect an entry to start from; preview them with a real one.
  const needsEntry = $derived(!!studio.previewDoc?.manifest.contexts.includes('entry'));
  let previewEntry = $state.raw<IEntry>();
  $effect(() => {
    if (!needsEntry || previewEntry) return;
    void projectContext.api.getEntries({
      count: 1,
      offset: 0,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
    }).then(entries => {
      previewEntry ??= entries[0];
    });
  });
  const previewEntryLabel = $derived(previewEntry ? writingSystemService.firstHeadword(previewEntry) || $t`(no headword)` : $t`Pick an entry`);
</script>

<div class="flex flex-wrap items-center gap-2 px-4 py-2 text-sm text-muted-foreground">
  <Badge variant="secondary" class="text-primary">
    <Icon icon="i-mdi-eye-outline" />
    {$t`Preview`}
  </Badge>
  {#if studio.canCompareVersions}
    <div role="group" aria-label={$t`Version`} class="inline-flex gap-0.5 rounded-md bg-muted p-0.5">
      <Button size="xs" variant={studio.showSavedVersion ? 'secondary' : 'ghost'} aria-pressed={studio.showSavedVersion} onclick={() => studio.showSavedVersion = true}>
        {$t`Current version`}
      </Button>
      <Button size="xs" variant={studio.showSavedVersion ? 'ghost' : 'secondary'} aria-pressed={!studio.showSavedVersion} onclick={() => studio.showSavedVersion = false}>
        {$t`New version`}
      </Button>
    </div>
  {/if}
  <span>{$t`Your real dictionary. Changes it tries are shown, not saved.`}</span>
  <div class="grow"></div>
  {#if needsEntry}
    <EntryOrSensePicker title={$t`Preview with entry`} mode="only-entries" pick={(selection) => previewEntry = selection.entry}>
      {#snippet trigger({props})}
        <Button {...props} variant="outline" size="sm" icon="i-mdi-book-open-variant" title={$t`Entry the plugin starts from`}>
          <span class="max-w-40 truncate">{previewEntryLabel}</span>
        </Button>
      {/snippet}
    </EntryOrSensePicker>
  {/if}
  {#if !IsMobile.value}
    <Button
      variant="outline"
      size="sm"
      icon="i-mdi-cellphone"
      aria-pressed={phoneWidth}
      onclick={() => phoneWidth = !phoneWidth}
    >
      {$t`Phone width`}
    </Button>
  {/if}
  <Button variant="outline" size="sm" icon="i-mdi-refresh" disabled={!docUrl} onclick={() => reloadToken++}>{$t`Reload`}</Button>
</div>
<div class="mx-4 mb-4 flex min-h-0 grow justify-center">
  <div
    class="relative h-full w-full overflow-hidden rounded-lg border bg-background"
    class:max-w-[390px]={phoneWidth}
    data-testid="plugin-studio-preview"
  >
    {#if studio.loadState === 'loading' || (studio.needsConsent && !studio.savedApproval)}
      <div class="absolute inset-0 grid place-items-center text-muted-foreground">
        <Icon icon="i-mdi-loading" class="animate-spin size-8" />
      </div>
    {:else if studio.needsConsent}
      <PluginConsentCard pluginName={title} manifest={studio.previewDoc?.manifest} onApprove={() => studio.approveSaved()} onGoBack={() => void studio.leave()} />
    {:else if docUrl && studio.previewDoc && (!needsEntry || previewEntry)}
      {#key `${docUrl}|${previewEntry?.id ?? ''}`}
        <PluginSandbox
          pluginId="studio-preview"
          pluginName={title}
          {docUrl}
          manifest={studio.previewDoc.manifest}
          writeMode="preview"
          entryId={needsEntry ? previewEntry?.id : undefined}
          {reloadToken}
        />
      {/key}
    {:else if needsEntry}
      <div class="absolute inset-0 grid place-items-center p-6 text-center text-muted-foreground">
        {$t`This plugin runs from an entry's menu. Pick an entry to preview it with.`}
      </div>
    {:else}
      <div class="absolute inset-0 grid place-items-center p-6">
        <div class="max-w-sm space-y-2 text-center">
          <Icon icon="i-mdi-puzzle-outline" class="size-10 text-muted-foreground" />
          <div class="font-medium">{$t`Your plugin will run here`}</div>
          <p class="text-sm text-muted-foreground">
            {$t`Describe it and have an AI write it, or paste a plugin file you already have.`}
          </p>
        </div>
      </div>
    {/if}
  </div>
</div>
