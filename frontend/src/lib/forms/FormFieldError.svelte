<script lang="ts">
  interface Props {
    error: string | string[] | undefined;
    id: string;
  }

  const { error, id }: Props = $props();
</script>

{#if error}
  <label for={id} class="label">
    <span class="label-text-alt text-error">{typeof error === 'string' ? error : error.join(', ')}</span>
  </label>
{/if}
