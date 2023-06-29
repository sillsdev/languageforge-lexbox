<script lang="ts">
  export let error: string | string[] | undefined;
  export let id: string;
</script>

{#if error}
  <label for={id} class="label">
    <span class="label-text-alt text-error">{typeof error === 'string' ? error : error.join(', ')}</span>
  </label>
{/if}
