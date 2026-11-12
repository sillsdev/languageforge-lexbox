<script lang="ts">
  import {onMount} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import CreateProjectDialog from '../../home/CreateProjectDialog.svelte';

  let dialog = $state<CreateProjectDialog>();
  onMount(() => dialog?.openDialog());
</script>

<Button variant="outline" onclick={() => dialog?.openDialog()}>Open new project dialog</Button>
<CreateProjectDialog bind:this={dialog} refreshProjects={() => Promise.resolve()} />
