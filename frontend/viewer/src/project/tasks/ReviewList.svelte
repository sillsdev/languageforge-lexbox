<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import ListItem from '$lib/components/ListItem.svelte';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import type {TaskSubject} from './subject.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';

  const writingSystemService = useWritingSystemService();

  let {
    subjects,
    onFinish = () => {}
  }: {
    subjects: TaskSubject[];
    onFinish: () => void;
  } = $props();
  let selectedSubject = $state<TaskSubject>();
  let editOpen = $state(false);
  function editSubject(subject: TaskSubject) {
    selectedSubject = subject;
    editOpen = true;
  }
</script>
<div class="flex flex-col">
  {#each subjects as subject (subject)}
    <ListItem class="my-2" onclick={() => editSubject(subject)} icon="i-mdi-book-open-page-variant">
      <p>{writingSystemService.headword(subject.entry)}</p>
      <p>{subject.subject}</p>
    </ListItem>
  {/each}
  <Button class="m-2" onclick={() => onFinish()}>{$t`Finish`}</Button>
</div>
<EditEntryDialog bind:open={editOpen} entryId={selectedSubject?.entry.id}/>
