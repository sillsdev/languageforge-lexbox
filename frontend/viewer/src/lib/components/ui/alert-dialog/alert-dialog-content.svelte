<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {AlertDialog as AlertDialogPrimitive, type WithoutChild} from 'bits-ui';
  import AlertDialogOverlay from './alert-dialog-overlay.svelte';
  import {useDialogSharedRoot} from '../dialog-shared/dialog-shared-root.svelte';

  let {
    ref = $bindable(null),
    class: className,
    portalProps,
    ...restProps
  }: WithoutChild<AlertDialogPrimitive.ContentProps> & {
    portalProps?: AlertDialogPrimitive.PortalProps;
  } = $props();

  // This behaviour is duplicated in dialog-content.svelte
  const state = $derived(useDialogSharedRoot());
  const bottomDialog = $derived(state.index === 1);
  const topDialog = $derived(state.index && state.index >= state.openDialogs);
</script>

<AlertDialogPrimitive.Portal {...portalProps}>
  <!-- Only ever use the first/bottom dialog's overlay otherwise there's a flash due to transitions on the appearing/dissappearing overlays -->
  <AlertDialogOverlay class={cn(bottomDialog || 'hidden')} data-dialog-index={state.index} data-dialog-top={topDialog} />
  <AlertDialogPrimitive.Content
    bind:ref
    class={cn(
      'bg-background data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%] fixed left-[50%] top-[50%] z-50 grid translate-x-[-50%] translate-y-[-50%] gap-4 border p-6 shadow-lg duration-200 rounded-lg',
      'min-w-[min(calc(100%-32px),50rem)] max-w-[calc(100%-32px)]',
      /* Ensure the top dialog is above the shared overlay */
      topDialog && 'z-[51]',
      /* Ensure any other dialog is below the shared overlay */
      topDialog || 'z-[49]',
      className,
    )}
    {...restProps}
  />
</AlertDialogPrimitive.Portal>
