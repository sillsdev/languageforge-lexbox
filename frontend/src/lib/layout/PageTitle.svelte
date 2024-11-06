<script lang="ts">
  import SetTitle from './SetTitle.svelte';

  export let title: string;
</script>

<SetTitle {title} />

<h1 class="text-center text-2xl">
  {title}
</h1>
