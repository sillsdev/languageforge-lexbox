<script lang="ts" module>

  export type Activity = IProjectActivity & {
    previousTimestamp?: string;
  };
</script>

<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Editor from '$lib/components/editor';
  import {Switch} from '$lib/components/ui/switch';
  import {type IComplexFormComponent, type IEntry, type IExampleSentence, type IProjectActivity, type ISense} from '$lib/dotnet-types';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {t} from 'svelte-i18n-lingui';
  import type {ChangeWithContext} from './ActivityItem.svelte';
  import {formatJsonForUi} from './utils';
  import FormatRelativeDate from '$lib/components/ui/format/format-relative-date.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {Button} from '$lib/components/ui/button';
  import Headwords from '$lib/Headwords.svelte';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import {Link} from 'svelte-routing';

  const { change: changeWithContext }: { change: ChangeWithContext } = $props();

  const { activity, context } = $derived(changeWithContext);

  const multiWindowService = useMultiWindowService();
  const currentView = useCurrentView();

  const affectedEntry = $derived(context.affectedEntries.length === 1 ? context.affectedEntries[0] : undefined);

  let currentEntity = $derived.by(() => {
    if (!affectedEntry) return undefined;

    if (context.entityType === 'Entry') {
      return affectedEntry;
    } else if (context.entityType === 'Sense') {
      const senseId = (context.snapshot as ISense).id;
      return affectedEntry.senses.find((s) => s.id === senseId);
    } else if (context.entityType === 'ExampleSentence') {
      const exampleId = (context.snapshot as IExampleSentence).id;
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
    <DropdownMenu.Trigger class={cn('text-base w-fit mr-2 justify-between', deleted && 'pointer-events-none')}>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="sm">
          <Headwords {entry} placeholder={$t`Untitled`} />
          {#if !deleted}
            <Icon icon="i-mdi-dots-vertical" />
          {:else}
            <span class="text-destructive">
              {$t`(Deleted)`}
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
            <Link {...props} to="./browse?entryId={entry.id}">
              <Icon icon="i-mdi-link" />
              {$t`Go to ${pt($t`Entry`, $t`Word`, $currentView)}`}
            </Link>
          {/snippet}
        </DropdownMenu.Item>
      </DropdownMenu.Group>
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{/snippet}

{#if affectedEntry}
  <div class="@container">
    <div class="flex flex-wrap gap-2 @lg:grid @lg:grid-cols-[2fr_4fr_auto] mb-3 items-center content-center justify-center">
      {#if context.affectedEntries.length === 1}
        {@const entry = context.affectedEntries[0]}
        {@render entryButton(entry)}
      {/if}

      {#if currentEntity && !affectedEntry.deletedAt}
        <label class="cursor-pointer w-fit flex items-center gap-2 border rounded p-2 px-4 bg-secondary/25">
          <span>
            <FormatRelativeDate date={activity.timestamp}
                                live
                                actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
          </span>
          <Switch disabled={!currentEntity} bind:checked={selectedShowCurrent} />
          {$t`Current version`}
        </label>
      {/if}
    </div>
  </div>
{/if}

{#if context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <EntryEditorPrimitive modalMode readonly entry={(showCurrent ? currentEntity : context.snapshot) as IEntry}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
        <SenseEditorPrimitive readonly sense={(showCurrent ? currentEntity : context.snapshot) as ISense}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
        <ExampleEditorPrimitive readonly example={(showCurrent ? currentEntity : context.snapshot) as IExampleSentence}/>
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ComplexFormComponent'}
  {@const cfc = context.snapshot as IComplexFormComponent}
  {@const complexForm = context.affectedEntries.find(e => e.id === cfc.complexFormEntryId)}
  {@const component = context.affectedEntries.find(e => e.id === cfc.componentEntryId)}
  <div class="space-y-2">
    <div class="flex gap-x-2 items-baseline">
      <span class="label">{$t`Complex form:`}</span>
      {#if complexForm}
        {@render entryButton(complexForm)}
      {:else}
        {$t`Not found`}
      {/if}
    </div>
    <div class="flex gap-x-2 items-baseline">
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
    {formatJsonForUi(context.snapshot)}
  </div>
{/if}
