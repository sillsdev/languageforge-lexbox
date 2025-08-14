<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Dialog as DialogPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import * as Dialog from './index.js';
  import {XButton} from '../button';

  let {
    ref = $bindable(null),
    class: className,
    portalProps,
    children,
    hideClose = false,
    ...restProps
  }: WithoutChildrenOrChild<DialogPrimitive.ContentProps> & {
    portalProps?: DialogPrimitive.PortalProps;
    children: Snippet;
    hideClose?: boolean;
  } = $props();
</script>

<Dialog.Portal {...portalProps}>
  <Dialog.Overlay />
  <DialogPrimitive.Content
    bind:ref
    class={cn(
      'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%] bg-background fixed left-[50%] top-[50%] z-50 grid translate-x-[-50%] translate-y-[-50%] gap-4 border p-6 shadow-lg duration-200 sm:rounded-lg',
      'max-sm:min-h-full min-w-full max-h-full max-w-full sm:min-w-[min(calc(100%-32px),50rem)] sm:max-h-[calc(100%-16px)] sm:max-w-[calc(100%-32px)]',
      'overflow-y-auto',
      className,
    )}
    {...restProps}
  >
    {@render children?.()}
    {#if !hideClose}
      <DialogPrimitive.Close class="absolute right-4 top-4">
        {#snippet child({props})}
          <XButton {...props} />
        {/snippet}
      </DialogPrimitive.Close>
    {/if}
  </DialogPrimitive.Content>
</Dialog.Portal>
