<script lang="ts">
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import * as Popover from '$lib/components/ui/popover';
  import * as Drawer from '$lib/components/ui/drawer';
  import {buttonVariants} from '$lib/components/ui/button';
  import type {PopoverTriggerProps, WithChildren, WithoutChildren} from 'bits-ui';
  import {Icon} from '../ui/icon';
  import type {ComponentProps} from 'svelte';
  import {cn} from '$lib/utils';

  type TriggerSnippet = PopoverTriggerProps['child'];
  type ContentProps = WithoutChildren<ComponentProps<typeof Popover.Content>>;

  let {
    open = $bindable(false),
    children,
    title,
    trigger,
    contentProps,
  }: WithChildren<{
    open?: boolean,
    title?: string,
    trigger: TriggerSnippet,
    contentProps?: ContentProps,
  }> = $props();
</script>

{#if !IsMobile.value}
  <Popover.Root bind:open>
    <Popover.Trigger child={trigger} />
    <Popover.Content {...contentProps} class={cn('w-64 sm:mr-4', contentProps?.class)}>
      <div class="space-y-3">
        {#if title}
          <h3 class="font-medium">{title}</h3>
        {/if}

        {#if children}
          {@render children()}
        {/if}
      </div>
    </Popover.Content>
  </Popover.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger child={trigger} />
    <Drawer.Content {...contentProps}>
      <Drawer.Close class={buttonVariants({ variant: 'ghost', size: 'icon', class: 'absolute top-4 right-4 z-10' })}>
        <Icon icon="i-mdi-close" />
      </Drawer.Close>
      <div class="mx-auto w-full max-w-sm p-4 overflow-auto overscroll-contain">
        {#if title}
          <Drawer.Header>
            <Drawer.Title class="text-center">{title}</Drawer.Title>
          </Drawer.Header>
        {/if}
        {#if children}
          {@render children()}
        {/if}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
