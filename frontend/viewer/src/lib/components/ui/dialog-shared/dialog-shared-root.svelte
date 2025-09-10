<script module lang="ts">
  import {type DialogOverlayProps, type DialogContentProps} from 'bits-ui';

  let openDialogs = $state(0);

  type DialogSharedRootStateProps = {
    openDialogs: number;
    index: number | undefined;
    overlayProps: DialogOverlayProps;
    contentProps: DialogContentProps;
  };

  const dialogSharedRootContext = new Context<DialogSharedRootStateProps>('Dialog.Shared.Root');

  export function initDialogSharedRoot(props: DialogSharedRootStateProps): DialogSharedRootStateProps {
    return dialogSharedRootContext.set(props);
  }

  export function useDialogSharedRoot(): DialogSharedRootStateProps {
    return dialogSharedRootContext.get();
  }
</script>

<script lang="ts">
  import { cn } from '$lib/utils';
  import { Context, watch } from 'runed';
  import { onDestroy, type Snippet } from 'svelte';
  import {AppNotification} from '$lib/notifications/notifications';

  type Props = {
    open: boolean;
    children?: Snippet;
  };

  let {
    open,
    children,
  }: Props = $props();

  let index = $state<number>();
  let trigger = $state<HTMLElement>();

  watch(() => open, (curr, prev) => {
    onOpenChange(!!prev, !!curr);
  });

  onDestroy(() => {
    onOpenChange(!!open, false);
  });

  function onOpenChange(prev: boolean, curr: boolean) {
    if (curr === prev) return;

    if (curr) {
      openDialogs++;
      index = openDialogs;
      trigger = document.activeElement as typeof trigger;
    } else {
      openDialogs--;
      index = undefined;
      trigger = undefined;

      if (openDialogs < 0) {
        AppNotification.display('Dialog stack: Invalid state', {
          description: new Error().stack,
          type: 'warning',
        });
        openDialogs = 0;
      }
    }
  }

  const bottomDialog = $derived(index === 1);
  const topDialog = $derived(index && index >= openDialogs);
  const dataProps = $derived({
    'data-dialog-index': index,
    'data-dialog-top': topDialog,
    'data-dialog-bottom': bottomDialog,
  });
  const overlayProps = $derived<DialogOverlayProps>({
    // Only ever use the first/bottom dialog's overlay otherwise there's a flash due to transitions on the appearing/disappearing overlays
    class: cn('z-[49]', bottomDialog || 'hidden'),
    ...dataProps,
  });
  const contentProps = $derived<DialogContentProps>({
    // Ensure the top dialog is above the shared overlay and any other dialog is below it
    class: topDialog ? 'z-[50]' : 'z-[48]',
    // The docs claim that this is the default behaviour, but it only seems to actually work if using <Dialog.Trigger>
    onCloseAutoFocus: (event: Event) => {
      if (trigger) {
        trigger.focus();
        event.preventDefault();
      }
    },
    ...dataProps,
  });

  initDialogSharedRoot({
    get openDialogs() { return openDialogs; },
    get index() { return index; },
    get overlayProps() { return overlayProps; },
    get contentProps() { return contentProps; },
  });
</script>

{@render children?.()}
