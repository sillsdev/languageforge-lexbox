<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Editor from '$lib/components/editor';
  import {Switch} from '$lib/components/ui/switch';
  import {type IChangeContext, type IComplexFormComponent, type IEntry, type IExampleSentence, type IObjectWithId, type IProjectActivity, type ISense} from '$lib/dotnet-types';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
  import FormatRelativeDate from '$lib/components/ui/format/format-relative-date.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {Button} from '$lib/components/ui/button';
  import {pt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import {Link} from 'svelte-routing';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import DiffFieldRows from '$lib/entry-editor/diff-view/DiffFieldRows.svelte';
  import {isObjectType, objectDiffRows} from '$lib/entry-editor/diff-view/diff-fields';

  type Props = {
    activity: IProjectActivity,
    context: IChangeContext,
  }

  const { activity, context }: Props = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();

  const affectedEntry = $derived(context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined);

  // A deleted entity's snapshot is its last state marked deleted: the diff shows it as gone.
  function existing(snapshot: IObjectWithId | undefined): IObjectWithId | undefined {
    return snapshot && !snapshot.deletedAt ? snapshot : undefined;
  }
  // No before means the commit created the entity, no after means it deleted it.
  const before = $derived(existing(context.previousSnapshot));
  const after = $derived(existing(context.snapshot));
  const anySnapshot = $derived(context.snapshot ?? context.previousSnapshot);

  let currentEntity = $derived.by(() => {
    if (!affectedEntry || !anySnapshot) return undefined;

    if (context.entityType === 'Entry') {
      return affectedEntry;
    } else if (context.entityType === 'Sense') {
      const senseId = anySnapshot.id;
      return affectedEntry.senses.find((s) => s.id === senseId);
    } else if (context.entityType === 'ExampleSentence') {
      const exampleId = anySnapshot.id;
      for (const sense of affectedEntry.senses) {
        const example = sense.exampleSentences.find((ex) => ex.id === exampleId);
        if (example) return example;
      }
    }
    return undefined;
  });
  let selectedShowCurrent = $derived(false);
  let showCurrent = $derived(currentEntity && selectedShowCurrent);
</script>

{#snippet entryButton(entry: IEntry)}
  {@const deleted = Boolean(entry.deletedAt)}
  <DropdownMenu.Root>
    <DropdownMenu.Trigger class={cn('text-base w-fit mr-2 justify-between flex-wrap whitespace-break-spaces text-start min-h-max py-1.5', deleted && 'pointer-events-none')}>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="sm">
          <Headwords {entry} placeholder={$t`Untitled`} />
          {#if !deleted}
            <Icon icon="i-mdi-dots-vertical" />
          {:else}
            <span class="text-destructive font-normal">
              ({$t`Deleted`})
            </span>
          {/if}
        </Button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="start">
      <DropdownMenu.Group>
        {#if multiWindowService}
          <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entry.id)}>
            <Icon icon="i-mdi-open-in-new" />
            {$t`Open in new Window`}
          </DropdownMenu.Item>
        {/if}
        <DropdownMenu.Item class="cursor-pointer" onclick={e => e.preventDefault()}>
          {#snippet child({props})}
            <Link {...props} to="./browse?{entryBrowseParams(entry.id)}">
              <Icon icon="i-mdi-book-arrow-right-outline" />
              {$t`Go to ${pt($t`Entry`, $t`Word`, viewService.currentView)}`}
            </Link>
          {/snippet}
        </DropdownMenu.Item>
      </DropdownMenu.Group>
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{/snippet}

{#if affectedEntry || currentEntity}
  <div class="flex flex-wrap gap-2 mb-3 items-center content-center justify-between">
    {#if context.affectedEntries.length === 1}
      <!--
      If there are more than 1 affected entries (e.g. complex-form-components that link two entries together)
      then the preview should be more explicit about what role the entries have and thus should be handled below
      based on the entity type.
      -->
      {@const entry = context.affectedEntries[0]}
      {@render entryButton(entry)}
    {/if}

    <label class={cn(
      'w-fit flex items-center gap-2 border rounded p-2 px-4 bg-secondary/25',
      currentEntity && 'cursor-pointer')}>
      <FormatRelativeDate date={activity.timestamp}
                          live
                          actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
      <Switch disabled={!currentEntity} bind:checked={selectedShowCurrent} class="text-destructive" />
      {#if currentEntity}
        <span>{$t`Current version`}</span>
      {:else}
        <span class="text-destructive">{$t`Deleted`}</span>
      {/if}
    </label>
  </div>
{/if}

{#if !anySnapshot}
  <div class="text-muted-foreground p-4 text-center">
    {$t`Preview not available`}
  </div>
{:else if showCurrent && context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <EntryEditorPrimitive modalMode readonly entry={currentEntity as IEntry}/>
    </Editor.Grid>
  </Editor.Root>
{:else if showCurrent && context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
        <SenseEditorPrimitive readonly sense={currentEntity as ISense}/>
    </Editor.Grid>
  </Editor.Root>
{:else if showCurrent && context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
        <ExampleEditorPrimitive readonly example={currentEntity as IExampleSentence}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <DiffEntryPrimitive mode="changes" before={before as IEntry | undefined} after={after as IEntry | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
      <DiffSensePrimitive mode="changes" before={before as ISense | undefined} after={after as ISense | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
      <DiffExamplePrimitive mode="changes" before={before as IExampleSentence | undefined} after={after as IExampleSentence | undefined}/>
    </Editor.Grid>
  </Editor.Root>
{:else if isObjectType(context.entityType)}
  <Editor.Root>
    <Editor.Grid>
      <DiffFieldRows rows={objectDiffRows[context.entityType]} {before} {after}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ComplexFormComponent'}
  {@const cfc = anySnapshot as IComplexFormComponent}
  {@const complexForm = context.affectedEntries.find(e => e.id === cfc.complexFormEntryId)}
  {@const component = context.affectedEntries.find(e => e.id === cfc.componentEntryId)}
  <div class="space-y-2">
    <div class="flex flex-wrap gap-2 items-baseline">
      <span class="label">{$t`Complex form:`}</span>
      {#if complexForm}
        {@render entryButton(complexForm)}
      {:else}
        {$t`Not found`}
      {/if}
    </div>
    <div class="flex flex-wrap gap-2 items-baseline">
      <span class="label">{$t`Component:`}</span>
      {#if component}
        {@render entryButton(component)}
      {:else}
        {$t`Not found`}
      {/if}
    </div>
  </div>
{:else}
  <div class="whitespace-pre-wrap font-mono text-sm">
    {formatJsonForUi(anySnapshot)}
  </div>
{/if}
