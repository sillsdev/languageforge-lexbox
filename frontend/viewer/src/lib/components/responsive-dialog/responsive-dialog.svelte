<script lang="ts">
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Drawer from '$lib/components/ui/drawer';
  import {buttonVariants} from '$lib/components/ui/button';
  import type {PopoverTriggerProps, WithChildren, WithoutChildren} from 'bits-ui';
  import {Icon} from '../ui/icon';
  import type {ComponentProps} from 'svelte';
  import {cn} from '$lib/utils';

  type TriggerSnippet = PopoverTriggerProps['child'];
  type ContentProps = WithoutChildren<ComponentProps<typeof Dialog.Content>>;
  type Props = ComponentProps<typeof Drawer.Root> & {
    open?: boolean;
    title: string;
    trigger?: TriggerSnippet;
    contentProps?: ContentProps;
  };

  let {open = $bindable(false), children, title, trigger, contentProps, ...rest}: WithChildren<Props> = $props();
</script>

{#if !IsMobile.value}
  <Dialog.Root bind:open {...rest}>
    <Dialog.Trigger child={trigger} />
    <Dialog.Content {...contentProps} class={cn('min-h-auto', contentProps?.class)}>
      <Dialog.Header>
        <Dialog.Title>{title}</Dialog.Title>
      </Dialog.Header>
      {@render children?.()}
    </Dialog.Content>
  </Dialog.Root>
{:else}
  <Drawer.Root bind:open {...rest}>
    <Drawer.Trigger child={trigger} />
    <Drawer.Content {...contentProps}>
      <Drawer.Close class={buttonVariants({variant: 'ghost', size: 'icon', class: 'absolute top-4 right-4 z-10'})}>
        <Icon icon="i-mdi-close" />
      </Drawer.Close>
      <div class="mx-auto w-full p-4 overflow-auto overscroll-contain">
        <Drawer.Header>
          <Drawer.Title>{title}</Drawer.Title>
        </Drawer.Header>
        {@render children?.()}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
