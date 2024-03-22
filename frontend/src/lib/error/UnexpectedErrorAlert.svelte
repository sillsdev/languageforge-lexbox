<script lang="ts">
  import { beforeNavigate } from '$app/navigation';
  import {useDismiss, useError} from '.';
  import t from '$lib/i18n';
  import UnexpectedError from './UnexpectedError.svelte';
  import { onDestroy } from 'svelte';

  let dialog: HTMLDialogElement;
  const error = useError();
  const dismiss = useDismiss();
  beforeNavigate(dismiss);

  onDestroy(
    // subscribe() is more durable than reactive syntax
    error.subscribe((e) => {
      if (!dialog) return;
      e ? open() : close();
    })
  );

  function dismissClick(): void {
    close();
    dismiss();
  }

  function open(): void {
    dialog.showModal();
    dialog.classList.add('modal-open');
  }

  function close(): void {
    dialog.close();
    dialog.classList.remove('modal-open');
  }
</script>

<dialog bind:this={dialog} class="modal">
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
