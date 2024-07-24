<script lang="ts">
  import t from '$lib/i18n';
  import {type ErrorMessage} from '$lib/forms';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';

  export let entityName: string;
  export let isRemoveDialog = false;
  let modal: ConfirmModal;

  export async function prompt(deleteCallback: () => Promise<ErrorMessage>): Promise<boolean> {
    return await modal.open(deleteCallback);
  }
</script>
<ConfirmModal bind:this={modal}
              title={isRemoveDialog ? $t('delete_modal.remove', { entityName }) : $t('delete_modal.delete', { entityName })}
              submitText={isRemoveDialog ? $t('delete_modal.remove', { entityName }) : $t('delete_modal.delete', { entityName })}
              submitIcon="i-mdi-trash-can"
              submitVariant="btn-error"
              cancelText={isRemoveDialog ? $t('delete_modal.dont_remove') : $t('delete_modal.dont_delete')}>
  <slot/>
</ConfirmModal>
