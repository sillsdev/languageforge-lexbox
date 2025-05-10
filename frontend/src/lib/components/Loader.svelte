<script lang="ts">
  interface Props {
    loading?: boolean;
    size?: 'loading-xs';
  }

  const { loading = false, size }: Props = $props();
</script>

{#if loading}
  <span class="loading loading-spinner {size ?? ''}"></span>
{/if}
