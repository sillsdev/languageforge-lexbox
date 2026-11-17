<script lang="ts">
  import {onMount} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import NameDialog from '$lib/author/NameDialog.svelte';

  const {fromMenu = false}: {fromMenu?: boolean} = $props();
  let dialog = $state<NameDialog>();
  onMount(() => void dialog?.ask({fromMenu}));
</script>

<Button variant="outline" onclick={() => void dialog?.ask({fromMenu})}>Open name dialog</Button>
<NameDialog bind:this={dialog} />
