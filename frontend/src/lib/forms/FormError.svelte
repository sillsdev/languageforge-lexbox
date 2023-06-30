<script lang="ts">
  export let error: string | undefined = undefined;
</script>

{#if error}
  <span class="label text-lg text-error justify-end">
    {error}
  </span>
{/if}
