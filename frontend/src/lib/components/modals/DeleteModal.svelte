<script lang="ts">
  import Modal, { DialogResponse } from './Modal.svelte';
  import t from '$lib/i18n';
  import { type ErrorMessage, FormError } from '$lib/forms';
  import Loader from '../Loader.svelte';
  import { TrashIcon } from '$lib/icons';

  export let entityName: string;
  export let isRemoveDialog = false;
  let modal: Modal;
  let error: ErrorMessage = undefined;

  export async function prompt(deleteCallback: () => Promise<ErrorMessage>): Promise<boolean> {
    if ((await modal.openModal()) === DialogResponse.Cancel) {
      error = undefined;
      return false;
    }
    error = await deleteCallback();
    if (error) {
      return prompt(deleteCallback);
    }
    modal.close();
    error = undefined;
    return true;
  }
</script>

<Modal bind:this={modal} showCloseButton={false}>
  <h2 class="text-xl mb-2">
    {#if isRemoveDialog}
      {$t('delete_modal.remove', { entityName })}
    {:else}
      {$t('delete_modal.delete', { entityName })}
    {/if}
  </h2>
  <slot />
  <FormError {error} right />
  <svelte:fragment slot="actions" let:submitting>
    <button class="btn btn-error" on:click={() => modal.submitModal()}>
      <Loader loading={submitting} />
      {#if isRemoveDialog}
        {$t('delete_modal.remove', { entityName })}
      {:else}
        {$t('delete_modal.delete', { entityName })}
      {/if}
      <TrashIcon />
    </button>
    <button class="btn btn-nuetral" disabled={submitting} on:click={() => modal.cancelModal()}>
      {#if isRemoveDialog}
        {$t('delete_modal.dont_remove')}
      {:else}
        {$t('delete_modal.dont_delete')}
      {/if}
    </button>
  </svelte:fragment>
</Modal>
