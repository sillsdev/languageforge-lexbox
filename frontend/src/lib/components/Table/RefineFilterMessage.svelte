<script lang="ts">
  import t from '$lib/i18n';

  interface Props {
    total: number;
    showing: number;
  }

  const { total, showing }: Props = $props();
</script>

{#if showing < total}
  <div class="text px-6 py-4 text-secondary flex gap-2 items-center">
    <span class="i-mdi-creation-outline text-lg"></span>
    {$t('table.refine_filter', { remainingRows: total - showing })}
  </div>
{/if}
