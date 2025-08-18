<script lang="ts">
  import type {TaskSubject} from './tasks-service';
  import {Button} from '$lib/components/ui/button';
  import ListItem from '$lib/components/ListItem.svelte';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';

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
    <ListItem class="m-2" onclick={() => editSubject(subject)} icon="i-mdi-book-open-page-variant">
      <p>{subject.subject}</p>
    </ListItem>
  {/each}
  <Button class="m-2" onclick={() => onFinish()}>Finish</Button>
</div>
<EditEntryDialog bind:open={editOpen} entryId={selectedSubject?.entryId}/>
