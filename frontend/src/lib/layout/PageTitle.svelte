<script lang="ts">
  import SetTitle from './SetTitle.svelte';

  interface Props {
    title: string;
  }

  let { title }: Props = $props();
</script>

<SetTitle {title} />

<h1 class="text-center text-2xl">
  {title}
</h1>
