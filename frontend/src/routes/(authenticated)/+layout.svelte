<script lang="ts">
  import t from '$lib/i18n';
  import { AdminIcon } from '$lib/icons';
  import { AdminContent, AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';

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
      <AdminContent>
        <a href="/admin" class="btn btn-sm btn-accent hidden sm:inline-flex">
          {$t('page_names.admin_dashboard')}
          <span class="ml-2"><AdminIcon /></span>
        </a>
      </AdminContent>
    </div>

    <Content>
      <slot />
    </Content>
  </div>

  <AppMenu on:click={close} on:keydown={close} />
</div>
