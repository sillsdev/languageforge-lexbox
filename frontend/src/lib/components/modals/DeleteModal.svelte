<script lang="ts">
  import type { Snippet } from 'svelte';
  import t from '$lib/i18n';
  import {type ErrorMessage} from '$lib/forms';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';

  interface Props {
    entityName: string;
    isRemoveDialog?: boolean;
    children?: Snippet;
  }

  let { entityName, isRemoveDialog = false, children }: Props = $props();
  let modal: ConfirmModal = $state();

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
  {@render children?.()}
</ConfirmModal>
