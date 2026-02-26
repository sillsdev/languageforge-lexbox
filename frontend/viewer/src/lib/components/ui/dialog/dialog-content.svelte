<script lang="ts">
  import {cn, type WithoutChildrenOrChild} from '$lib/utils.js';
  import DialogPortal from './dialog-portal.svelte';
  import {Dialog as DialogPrimitive, mergeProps} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import * as Dialog from './index.js';
  import {XButton} from '../button';
  import {useDialogSharedRoot} from '../dialog-shared/dialog-shared-root.svelte';
  import type {ComponentProps} from 'svelte';

  let {
    ref = $bindable(null),
    class: className,
    portalProps,
    children,
    hideClose = false,
    ...restProps
  }: WithoutChildrenOrChild<DialogPrimitive.ContentProps> & {
    portalProps?: WithoutChildrenOrChild<ComponentProps<typeof DialogPortal>>;
    children: Snippet;
    hideClose?: boolean;
  } = $props();

  // This is duplicated in alert-dialog-content.svelte
  const state = useDialogSharedRoot();
</script>

<DialogPortal {...portalProps}>
  <Dialog.Overlay {...state.overlayProps} />
  <DialogPrimitive.Content
    bind:ref
    data-slot="dialog-content"
    {...mergeProps(state.contentProps, restProps)}
    class={cn(
      'bg-background data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed top-[50%] left-[50%] z-50 grid translate-x-[-50%] translate-y-[-50%] gap-4 border p-6 shadow-lg duration-200 sm:rounded-lg',
      'max-sm:min-h-full min-w-full max-h-full max-w-full sm:min-w-[min(calc(100%-32px),50rem)] sm:max-h-[calc(100%-16px)] sm:max-w-[calc(100%-32px)]',
      'overflow-y-auto',
      state.contentProps.class,
      className,
    )}
  >
    {@render children?.()}
    {#if !hideClose}
      <DialogPrimitive.Close class="absolute end-4 top-4">
        {#snippet child({props})}
          <XButton {...props} />
        {/snippet}
      </DialogPrimitive.Close>
    {/if}
  </DialogPrimitive.Content>
</DialogPortal>
