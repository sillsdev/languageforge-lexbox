<script lang="ts">
  import { AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';

  let menuToggle = false;

  function open(): void {
    menuToggle = true;
  }

  function close(): void {
    menuToggle = false;
  }

  function closeOnEscape(event: KeyboardEvent): void {
    event.key === 'Escape' && close();
  }
</script>

<svelte:window on:keydown={closeOnEscape} />

<div class="drawer drawer-end">
  <input type="checkbox" checked={menuToggle} class="drawer-toggle" />

  <div class="drawer-content">
    <AppBar on:menuopen={open} />
    <div class="bg-secondary p-2 pl-6 flex justify-between items-center">
      <Breadcrumbs />
    </div>

    <Content>
      <slot />
    </Content>
  </div>

  <AppMenu on:click={close} on:keydown={close} />
</div>
