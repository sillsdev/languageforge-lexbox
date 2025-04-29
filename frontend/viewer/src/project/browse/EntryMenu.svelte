<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { type IconClass } from '$lib/icon-class';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Drawer from '$lib/components/ui/drawer';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import { buttonVariants } from '$lib/components/ui/button';
  import Button from '$lib/components/ui/button/button.svelte';

  let { onDelete } = $props<{
    onDelete?: () => void;
  }>();

  let open = $state(false);
  const triggerVariant = buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' });
</script>

{#snippet items()}
  {@render menuItem('i-mdi-delete', $t`Delete Entry`, onDelete)}
  {@render menuItem('i-mdi-history', $t`History`, () => {})}
{/snippet}

{#snippet menuItem(icon: IconClass, label: string, onSelect: () => void)}
  {#if !IsMobile.value}
    <DropdownMenu.Item class="cursor-pointer" {onSelect}>
      <Icon {icon} class="mr-2" />
      {label}
    </DropdownMenu.Item>
  {:else}
    <Button
      onclick={onSelect}
      variant="ghost"
      class="w-full justify-start"
      {icon}
    >
      {label}
    </Button>
  {/if}
{/snippet}

{#if !IsMobile.value}
  <DropdownMenu.Root bind:open>
    <DropdownMenu.Trigger class={triggerVariant}>
      {#snippet child({props})}
        <Button {...props} size="icon" variant="ghost" icon="i-mdi-dots-vertical" />
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end">
      {@render items()}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger class={triggerVariant}>
      <Icon icon="i-mdi-dots-vertical" class="cursor-pointer" />
    </Drawer.Trigger>
    <Drawer.Content>
      <div class="mx-auto w-full max-w-sm p-4">
        <Drawer.Header class="justify-items-end">
          <Drawer.Close class={buttonVariants({ variant: 'ghost', size: 'icon' })}><Icon icon="i-mdi-close" /></Drawer.Close>
        </Drawer.Header>
        <div class="space-y-2 flex flex-col gap-2">
          {@render items()}
        </div>
        <Drawer.Footer>
        </Drawer.Footer>
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
