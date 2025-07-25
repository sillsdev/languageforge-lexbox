<script lang="ts" module>
  export const enum DialogResponse {
    Cancel = 'cancel',
    Submit = 'submit',
  }
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  import t from '$lib/i18n';
  import Notify from '$lib/notify/Notify.svelte';
  import { OverlayContainer } from '$lib/overlay';
  import { writable } from 'svelte/store';

  let dialogResponse = writable<DialogResponse | null>(null);
  let open = writable(false);
  let closing = $derived($dialogResponse !== null && $open);
  let submitting = $derived($dialogResponse === DialogResponse.Submit && $open);
  interface Props {
    bottom?: boolean;
    showCloseButton?: boolean;
    closeOnClickOutside?: boolean;
    hideActions?: boolean;
    onClose?: (reponse: DialogResponse) => void;
    onOpen?: () => void;
    onSubmit?: () => void;
    children?: Snippet<[unknown]>;
    actions?: Snippet<[{ submitting: boolean; closing?: boolean; close: () => void }]>;
    extraActions?: Snippet;
  }

  const {
    bottom = false,
    showCloseButton = true,
    closeOnClickOutside = true,
    hideActions = false,
    onClose,
    onOpen,
    onSubmit,
    children,
    actions,
    extraActions,
  }: Props = $props();

  export async function openModal(autoCloseOnCancel = true, autoCloseOnSubmit = false): Promise<DialogResponse> {
    $dialogResponse = null;
    $open = true;
    onOpen?.();
    const response = await new Promise<DialogResponse>((resolve) => {
      const unsub = dialogResponse.subscribe((reason) => {
        if (reason) {
          unsub();
          resolve(reason);
        }
      });
    });
    if (autoCloseOnCancel && response === DialogResponse.Cancel) {
      close();
    }
    if (autoCloseOnSubmit && response === DialogResponse.Submit) {
      close();
    }
    return response;
  }
  export function cancelModal(): void {
    $open = false;
    $dialogResponse = DialogResponse.Cancel;
  }
  export function submitModal(): Promise<void> {
    $dialogResponse = DialogResponse.Submit;
    //a promise that will resolve when the modal is closed, or openModal is called again
    return new Promise<void>((resolve) => {
      const unsubOpen = open.subscribe((open) => {
        if (!open) {
          unsubOpen();
          resolve();
        }
      });
      const unsubResponse = dialogResponse.subscribe((reason) => {
        if (reason === null) {
          unsubResponse();
          resolve();
        }
      });
    });
  }

  export function close(): void {
    $open = false;
  }

  $effect(() => {
    if ($dialogResponse === DialogResponse.Submit) {
      onSubmit?.();
    }
  });
  $effect(() => {
    if (!$open && $dialogResponse !== null) {
      onClose?.($dialogResponse);
    }
  });
  let dialog: HTMLDialogElement | undefined = $state();
  //dialog will still work if the browser doesn't support it, but this enables focus trapping and other features
  $effect(() => {
    if (dialog) {
      if ($open) {
        //showModal might be undefined if the browser doesn't support dialog
        dialog.showModal?.call(dialog);
      } else {
        dialog.close?.call(dialog);
      }
    }
  });
</script>

{#if $open}
  <!-- using DaisyUI modal https://daisyui.com/components/modal/ -->
  <dialog
    bind:this={dialog}
    class="modal justify-items-center"
    class:modal-bottom={bottom}
    oncancel={cancelModal}
    onclose={cancelModal}
  >
    <OverlayContainer />

    <div class="modal-box max-w-3xl">
      {#if showCloseButton}
        <button class="btn btn-sm btn-circle absolute right-2 top-2 z-10" aria-label={$t('close')} onclick={cancelModal}
          >✕
        </button>
      {/if}
      {@render children?.({ closing, submitting })}
      {#if actions && !hideActions}
        <div class="modal-action">
          <div class="flex gap-4">
            {@render extraActions?.()}
          </div>
          <div class="flex gap-4">
            {@render actions?.({ closing, submitting, close })}
          </div>
        </div>
      {/if}
    </div>
    {#if closeOnClickOutside}
      <form method="dialog" class="modal-backdrop">
        <button>invisible</button>
      </form>
    {/if}
    <Notify />
  </dialog>
{/if}

<style>
  .modal-action {
    justify-content: var(--justify-actions, space-between);
  }
</style>
