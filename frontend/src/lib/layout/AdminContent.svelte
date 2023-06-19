<script>
  import { isAdmin } from '$lib/user';
  import { page } from '$app/stores'
</script>

{#if isAdmin($page.data.user)}
    <slot />
{/if}
