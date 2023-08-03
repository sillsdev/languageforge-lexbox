<script lang="ts">
  import NavBalls from './NavBalls.svelte';
  import PageHeader from './PageHeader.svelte';
</script>

<div class="md:px-8 md:max-w-2xl md:mx-auto">
  <div class="grid grid-cols-3">
    <span />
    <PageHeader>
      <slot name="header" />
    </PageHeader>
    <div class="justify-self-end">
      <NavBalls />
    </div>
  </div>

  <main class="py-6">
    <slot />
  </main>
</div>
