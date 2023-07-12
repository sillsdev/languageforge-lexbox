<script lang="ts">
  export let error: string | undefined = undefined;
  export let right = false; // when the form submit button is on the right
</script>

{#if error}
  <span class="label text-lg text-error" class:justify-end={right}>
    {error}
  </span>
{/if}
