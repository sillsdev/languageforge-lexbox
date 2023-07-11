<script lang="ts">
  import { beforeNavigate } from '$app/navigation';
  import { dismiss, error } from '.';
  import { onDestroy } from 'svelte';
  import { t } from 'svelte-intl-precompile';
  import UnexpectedError from './UnexpectedError.svelte';

  let dialog: HTMLDialogElement;

  beforeNavigate(dismiss);

  onDestroy(
    error.subscribe((e) => {
      if (!dialog) return;
      (e) ? open() : close();
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

<dialog bind:this={dialog} class="modal modal-bottom alert-error max-w-[95vw]">
  <div class="modal-box alert-error">
    <UnexpectedError />
    <div class="flex justify-end modal-action">
      <button on:click={dismissClick} class="btn btn-ghost self-end">{$t('modal.dismiss')}</button>
    </div>
  </div>
</dialog>
