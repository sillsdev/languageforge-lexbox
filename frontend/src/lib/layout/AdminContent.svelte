<script>
  import { isAdmin } from '$lib/user';
</script>

{#if $isAdmin}
    <slot />
{/if}
