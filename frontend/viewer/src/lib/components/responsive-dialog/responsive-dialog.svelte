<script lang="ts">
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Drawer from '$lib/components/ui/drawer';
  import { buttonVariants } from '$lib/components/ui/button';
  import type {PopoverTriggerProps, WithChildren} from 'bits-ui';
  import {Icon} from '../ui/icon';

  type TriggerSnippet = PopoverTriggerProps['child'];

  let { open = $bindable(false), children, title, trigger }: WithChildren<{ open?: boolean, title: string, trigger?: TriggerSnippet }> = $props();
</script>

{#if !IsMobile.value}
  <Dialog.Root bind:open>
    <Dialog.Trigger child={trigger} />
    <Dialog.Content class="min-h-auto">
      <Dialog.Header>
        <Dialog.Title>{title}</Dialog.Title>
      </Dialog.Header>
      {@render children?.()}
    </Dialog.Content>
  </Dialog.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger child={trigger} />
    <Drawer.Content>
      <Drawer.Close class={buttonVariants({ variant: 'ghost', size: 'icon', class: 'absolute top-4 right-4 z-10' })}>
        <Icon icon="i-mdi-close" />
      </Drawer.Close>
      <div class="mx-auto w-full max-w-sm_ p-4">
        <Drawer.Header>
          <Drawer.Title>{title}</Drawer.Title>
        </Drawer.Header>
        {@render children?.()}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
