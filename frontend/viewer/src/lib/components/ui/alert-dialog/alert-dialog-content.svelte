<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {AlertDialog as AlertDialogPrimitive, mergeProps, type WithoutChild} from 'bits-ui';
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

  // This is duplicated in dialog-content.svelte
  const state = $derived(useDialogSharedRoot());
</script>

<AlertDialogPrimitive.Portal {...portalProps}>
  <AlertDialogOverlay {...state.overlayProps} />
  <AlertDialogPrimitive.Content
    bind:ref
    {...mergeProps(state.contentProps, restProps)}
    class={cn(
      'bg-background data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%] fixed left-[50%] top-[50%] z-50 grid translate-x-[-50%] translate-y-[-50%] gap-4 border p-6 shadow-lg duration-200 rounded-lg',
      'min-w-[min(calc(100%-32px),50rem)] max-w-[calc(100%-32px)]',
      state.contentProps.class,
      className,
    )}
  />
</AlertDialogPrimitive.Portal>
