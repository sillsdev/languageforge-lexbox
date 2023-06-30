<script lang="ts">
  export let icon: string;
  export let disabled = false;
  export let loading = false;
  export let ghost = false;
</script>

<button {disabled} on:click class="btn btn-square {ghost ? 'btn-ghost' : 'btn-outline'}" class:loading>
  {#if !loading}
    <span class="{icon} text-lg" />
  {/if}
</button>
