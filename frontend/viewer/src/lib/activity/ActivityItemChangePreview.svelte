<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Editor from '$lib/components/editor';
  import {type IChangeContext, type IComplexFormComponent, type IEntry, type IExampleSentence, type ISense} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {formatJsonForUi} from './utils';
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
  import DiffVocabPrimitive from '$lib/entry-editor/diff-view/DiffVocabPrimitive.svelte';

  type Props = {
    context: IChangeContext,
  }

  const { context }: Props = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();

</script>

{#snippet entryButton(entry: IEntry)}
  {@const deleted = Boolean(entry.deletedAt)}
  <DropdownMenu.Root>
    <DropdownMenu.Trigger class={cn('text-base w-fit mr-2 justify-between flex-wrap whitespace-break-spaces text-start min-h-max py-1.5', deleted && 'pointer-events-none')}>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="sm">
          <Headwords {entry} showHomograph placeholder={$t`Untitled`} />
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
              <Icon icon="i-mdi-link" />
              {$t`Go to ${pt($t`Entry`, $t`Word`, viewService.currentView)}`}
            </Link>
          {/snippet}
        </DropdownMenu.Item>
      </DropdownMenu.Group>
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{/snippet}

{#if context.affectedEntries.length === 1}
  <div class="flex flex-wrap gap-2 mb-3 items-center">
    {@render entryButton(context.affectedEntries[0])}
  </div>
{/if}

{#if !context.snapshot && !context.previousSnapshot}
  <div class="text-muted-foreground p-4 text-center">
    {$t`Preview not available for this type of change`}
  </div>
{:else if context.entityType === 'Entry'}
  <Editor.Root>
    <Editor.Grid>
      <DiffEntryPrimitive before={context.previousSnapshot as IEntry | undefined} after={context.snapshot as IEntry | undefined} />
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'Sense'}
  <Editor.Root>
    <Editor.Grid>
      <DiffSensePrimitive before={context.previousSnapshot as ISense | undefined} after={context.snapshot as ISense | undefined} />
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ExampleSentence'}
  <Editor.Root>
    <Editor.Grid>
      <DiffExamplePrimitive before={context.previousSnapshot as IExampleSentence | undefined} after={context.snapshot as IExampleSentence | undefined} />
    </Editor.Grid>
  </Editor.Root>
{:else if context.entityType === 'ComplexFormComponent'}
  {@const cfc = (context.snapshot ?? context.previousSnapshot) as IComplexFormComponent}
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
{:else if context.entityType === 'PartOfSpeech'
  || context.entityType === 'SemanticDomain'
  || context.entityType === 'Publication'
  || context.entityType === 'ComplexFormType'
  || context.entityType === 'MorphType'
  || context.entityType === 'WritingSystem'
  || context.entityType === 'CustomView'}
  <Editor.Root>
    <Editor.Grid>
      <DiffVocabPrimitive before={context.previousSnapshot} after={context.snapshot} />
    </Editor.Grid>
  </Editor.Root>
{:else}
  <div class="whitespace-pre-wrap font-mono text-sm">
    {formatJsonForUi(context.snapshot ?? context.previousSnapshot ?? {})}
  </div>
{/if}
