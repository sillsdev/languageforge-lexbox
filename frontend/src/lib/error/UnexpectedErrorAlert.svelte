<script lang="ts">
  import { beforeNavigate } from '$app/navigation';
  import {useDismiss, useError} from '.';
  import t from '$lib/i18n';
  import UnexpectedError from './UnexpectedError.svelte';
  import { onDestroy } from 'svelte';
  import { browser } from '$app/environment';

  let dialog: HTMLDialogElement;
  const error = useError();
  const dismiss = useDismiss();
  beforeNavigate(dismiss);

  onDestroy(
    // subscribe() is more durable than reactive syntax
    error.subscribe((e) => {
      dialog = dialog ?? (browser ? document.querySelector('.error-alert') : undefined) ?? undefined;
      if (!dialog) return;
      e ? open() : close();
    })
  );

  function dismissClick(): void {
    close();
    dismiss();
  }

  function open(): void {
    dialog.showModal?.call(dialog);
    dialog.classList.add('modal-open');
  }

  function close(): void {
    dialog.close?.call(dialog);
    dialog.classList.remove('modal-open');
  }
</script>

<dialog bind:this={dialog} class="modal error-alert">
  <div class="modal-box bg-error text-error-content max-w-[95vw] w-[unset]">
    <UnexpectedError />
    <div class="flex justify-end gap-4 modal-action">
      <a class="btn btn-outline" href="/" rel="external">
        {$t('errors.go_home')}
        <span class="i-mdi-home-outline text-xl"></span>
      </a>
      <button on:click={dismissClick} class="btn btn-outline self-end">
        {$t('modal.dismiss')}
        <span class="i-mdi-close text-xl"></span>
      </button>
    </div>
  </div>
</dialog>
