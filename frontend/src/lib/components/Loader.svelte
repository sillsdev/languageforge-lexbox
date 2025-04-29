<script lang="ts">
  interface Props {
    loading?: boolean;
    size?: undefined | 'loading-xs';
  }

  let { loading = false, size = undefined }: Props = $props();
</script>

{#if loading}
  <span class="loading loading-spinner {size ?? ''}"></span>
{/if}
