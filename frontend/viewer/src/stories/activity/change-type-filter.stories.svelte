<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ChangeTypeFilter from '$lib/activity/ChangeTypeFilter.svelte';
  import AuthorFilter from '$lib/activity/AuthorFilter.svelte';
  import type {IActivityAuthor, IActivityChangeType} from '$lib/dotnet-types';
  import {fwliteStoryParameters} from '../fwl-parameters';

  const {Story} = defineMeta({
    title: 'activity/change-type-filter',
    parameters: fwliteStoryParameters({}),
  });

  // A realistic slice of the generated change types across every filter section.
  const changeTypes: IActivityChangeType[] = [
    {key: 'CreateEntryChange', label: 'Create entry', commitCount: 12},
    {key: 'jsonPatch:Entry', label: 'Edit entry', commitCount: 40},
    {key: 'delete:Entry', label: 'Delete entry', commitCount: 3},
    {key: 'AddEntryComponentChange', label: 'Add entry component', commitCount: 2},
    {key: 'CreateSenseChange', label: 'Create sense', commitCount: 9},
    {key: 'jsonPatch:Sense', label: 'Edit sense', commitCount: 22},
    {key: 'SetPartOfSpeechChange', label: 'Set part of speech', commitCount: 6},
    {key: 'AddSemanticDomainChange', label: 'Add semantic domain', commitCount: 4},
    {key: 'CreateExampleSentenceChange', label: 'Create example sentence', commitCount: 5},
    {key: 'jsonPatch:ExampleSentence', label: 'Edit example sentence', commitCount: 7},
    {key: 'create:remote-resource', label: 'Create remote resource', commitCount: 2},
    {key: 'CreateUserCommentChange', label: 'Create user comment', commitCount: 3},
    {key: 'CreatePartOfSpeechChange', label: 'Create part of speech', commitCount: 1},
    {key: 'CreateSemanticDomainChange', label: 'Create semantic domain', commitCount: 100},
    {key: 'jsonPatch:WritingSystem', label: 'Edit writing system', commitCount: 2},
  ];
</script>

<script lang="ts">
  let selection = $state<string[]>([]);
  let partialSelection = $state<string[]>(['CreateSenseChange', 'jsonPatch:Sense']);
  const authors: IActivityAuthor[] = [
    {authorId: undefined, authorName: undefined, commitCount: 3},
    {authorId: 'a1', authorName: 'Alice', commitCount: 40},
    {authorId: 'b1', authorName: 'Bob', commitCount: 12},
    {authorId: undefined, authorName: 'FieldWorks', commitCount: 7},
  ];
</script>

<Story name="Empty selection (no filter)">
  {#snippet template()}
    <div class="w-64">
      <ChangeTypeFilter {changeTypes} selected={selection} onSelectionChange={(keys) => (selection = keys)} />
    </div>
  {/snippet}
</Story>

<Story name="Author filter (same shell)">
  {#snippet template()}
    <div class="w-64">
      <AuthorFilter {authors} selected={['a1']} onSelectionChange={() => {}} />
    </div>
  {/snippet}
</Story>

<Story name="Partial selection (auto-expands its group)">
  {#snippet template()}
    <div class="w-64">
      <ChangeTypeFilter {changeTypes} selected={partialSelection} onSelectionChange={(keys) => (partialSelection = keys)} />
    </div>
  {/snippet}
</Story>
