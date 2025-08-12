<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Dialog as DialogPrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import * as Dialog from './index.js';
  import {XButton} from '../button';
  import {useDialogSharedRoot} from '../dialog-shared/dialog-shared-root.svelte';

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

  // This behaviour is duplicated in alert-dialog-content.svelte
  const state = $derived(useDialogSharedRoot());
  const bottomDialog = $derived(state.index === 1);
  const topDialog = $derived(state.index && state.index >= state.openDialogs);
</script>

<Dialog.Portal {...portalProps}>
  <!-- Only ever use the first/bottom dialog's overlay otherwise there's a flash due to transitions on the appearing/dissappearing overlays -->
  <Dialog.Overlay class={cn(bottomDialog || 'hidden')} data-dialog-index={state.index} data-dialog-top={topDialog} />
  <DialogPrimitive.Content
    bind:ref
    class={cn(
      'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%] bg-background fixed left-[50%] top-[50%] z-50 grid translate-x-[-50%] translate-y-[-50%] gap-4 border p-6 shadow-lg duration-200 sm:rounded-lg',
      'min-h-full min-w-full max-h-full max-w-full sm:min-h-[min(calc(100%-16px),30rem)] sm:min-w-[min(calc(100%-32px),50rem)] sm:max-h-[calc(100%-16px)] sm:max-w-[calc(100%-32px)]',
      'overflow-y-auto',
      /* Ensure the top dialog is above the shared overlay */
      topDialog && 'z-[51]',
      /* Ensure any other dialog is below the shared overlay */
      topDialog || 'z-[49]',
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
