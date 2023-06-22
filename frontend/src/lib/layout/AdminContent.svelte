<script>
  import { isAdmin } from '$lib/user';
  import { page } from '$app/stores'
</script>
<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if isAdmin($page.data.user)}
    <slot />
{/if}
