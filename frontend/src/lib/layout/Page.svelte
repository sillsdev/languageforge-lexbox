<script>
  import PageHeader from './PageHeader.svelte';
</script>

<PageHeader>
  <slot name="header" />
</PageHeader>

<main class="md:px-8 pt-6 md:mx-auto md:max-w-2xl">
  <slot />
</main>
