<script lang="ts">
  // This script handles the account settings page.
  // For now, it only allows you to modify the users display name
  // Minimal changes allow it to modify the users email as well (but this should have a re-verification system)
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { FormModal } from '$lib/components/modals';
  import { _deleteUserByAdminOrSelf } from '../../routes/(authenticated)/(dashboards)/admin/+page';
  import type { DeleteUserByAdminOrSelfInput } from '$lib/gql/types';
  import type { FormModalResult } from '$lib/components/modals/FormModal.svelte';

  const verify = z.object({
    keyphrase: z.string().refine((value) => value.match(`^${$t('admin_dashboard.enter_to_delete.user.value')}$`)),
  });
  type Schema = typeof verify;

  let deletionFormModal: FormModal<Schema>;
  $: deletionForm = deletionFormModal?.form();
  export async function open(id: string):  Promise<FormModalResult<Schema>> {
    return await deletionFormModal.open(async () => {
      const deleteUserInput: DeleteUserByAdminOrSelfInput = {
        userId: id,
      };
      const { error } = await _deleteUserByAdminOrSelf(deleteUserInput);
      return error?.message;
    });
  }
</script>

<FormModal bind:this={deletionFormModal} schema={verify} let:errors>
  <span slot="title">{$t('admin_dashboard.form_modal.delete_user')}</span>
  <Input
    id="keyphrase"
    type="text"
    label={$t('admin_dashboard.enter_to_delete.user.label')}
    error={errors.keyphrase}
    bind:value={$deletionForm.keyphrase}
  />
  <span slot="submitText">{$t('admin_dashboard.form_modal.delete_user')}</span>
</FormModal>
