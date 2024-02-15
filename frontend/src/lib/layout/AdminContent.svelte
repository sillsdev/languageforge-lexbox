<script>
  import { page } from '$app/stores'
</script>
<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if $page.data.user?.isAdmin}
    <slot />
{/if}
