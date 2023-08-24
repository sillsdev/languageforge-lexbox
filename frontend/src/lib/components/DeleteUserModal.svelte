<script lang="ts">
  import type { I18nShapeKey } from '$lib/i18n';
  import type { DeleteUserByAdminOrSelfInput } from '$lib/gql/types';
  import ConfirmDeleteModal, { type DeleteModalI18nShape } from './modals/ConfirmDeleteModal.svelte';
  import { _deleteUserByAdminOrSelf } from '$lib/gql/mutations';

  export let i18nScope: I18nShapeKey<DeleteModalI18nShape>;

  let modal: ConfirmDeleteModal;

  export async function open(user: {id: string, name: string}): ReturnType<(typeof modal)['open']> {
    return await modal.open(user.name, async () => {
      const deleteUserInput: DeleteUserByAdminOrSelfInput = {
        userId: user.id,
      };
      const { error } = await _deleteUserByAdminOrSelf(deleteUserInput);
      return error?.message;
    });
  }
</script>

<ConfirmDeleteModal bind:this={modal} {i18nScope} />
