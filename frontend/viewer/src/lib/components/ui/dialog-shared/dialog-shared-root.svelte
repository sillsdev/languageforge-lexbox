<script module lang="ts">

  let openDialogs = $state(0);

  type DialogSharedRootStateProps = {
    openDialogs: number;
    index?: number;
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
  import {Context, watch} from 'runed';
  import {onDestroy, type Snippet} from 'svelte';

  let {
    open,
    children,
  }: {
    open: boolean;
    children?: Snippet;
  } = $props();

  let index = $state<number | undefined>(undefined);

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
    } else {
      openDialogs--;
      index = undefined;

      if (openDialogs < 0) {
        console.warn(`DialogSharedRoot: openDialogs went below 0 (${openDialogs}), resetting to 0`);
        openDialogs = 0;
      }
    }
  }

  initDialogSharedRoot({
    get openDialogs() { return openDialogs; },
    get index() { return index; },
  });
</script>

{@render children?.()}
