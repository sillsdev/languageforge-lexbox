<script lang="ts">
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import * as Popover from '$lib/components/ui/popover';
  import * as Drawer from '$lib/components/ui/drawer';
  import { buttonVariants } from '$lib/components/ui/button';
  import { t } from 'svelte-i18n-lingui';
  import type {WithChildren} from 'bits-ui';
  import type {Snippet} from 'svelte';
  const isMobile = new IsMobile();
  let { open = $bindable(false), children, title, trigger }: WithChildren<{ open?: boolean, title: string, trigger: Snippet }> = $props();
</script>

{#if !isMobile.current}
  <Popover.Root bind:open>
    <Popover.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
      {@render trigger()}
    </Popover.Trigger>
    <Popover.Content class="w-64 sm:mr-4">
      <div class="space-y-3">
        <h3 class="font-medium">{title}</h3>
        {#if children}
          {@render children()}
        {/if}
      </div>
    </Popover.Content>
  </Popover.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
      {@render trigger()}
    </Drawer.Trigger>
    <Drawer.Content>
      <div class="mx-auto w-full max-w-sm p-4">
        <Drawer.Header>
          <Drawer.Title>{title}</Drawer.Title>
        </Drawer.Header>
        {#if children}
          {@render children()}
        {/if}
        <Drawer.Footer>
          <Drawer.Close class={buttonVariants({ variant: 'outline' })}>{$t`Close`}</Drawer.Close>
        </Drawer.Footer>
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
