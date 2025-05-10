<script lang="ts">
  interface Props {
    value?: string;
  }

  const { value = '' }: Props = $props();
</script>

<svelte:head>
  <title>{value}</title>
</svelte:head>
