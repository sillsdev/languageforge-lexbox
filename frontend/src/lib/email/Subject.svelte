<script lang="ts">
  interface Props {
    value?: string;
  }

  let { value = '' }: Props = $props();
</script>

<svelte:head>
  <title>{value}</title>
</svelte:head>
