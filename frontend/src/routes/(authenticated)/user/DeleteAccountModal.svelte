<script lang="ts">
  // This script handles the account settings page.
  // For now, it only allows you to modify the users display name
  // Minimal changes allow it to modify the users email as well (but this should have a re-verification system)
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import { z } from 'zod';
  import { FormModal } from '$lib/components/modals';


  const verify = z.object({
    keyphrase: z.string().refine((value) => value.match(`^${$t('account_settings.keyphrase')}$`)),
  });
  export let deleteUser: CallableFunction;
  let deletionFormModal: FormModal<typeof verify>;
  $: deletionForm = deletionFormModal?.form();
  export async function open(): Promise<void> {
    await deletionFormModal.open(async () => {
        await deleteUser();
    });
  }
</script>

<FormModal bind:this={deletionFormModal} schema={verify} let:errors>
  <span slot="title">{$t('account_settings.delete_account')}</span>
  <Input
    id="keyphrase"
    type="text"
    label={$t('account_settings.delete_user_label')}
    error={errors.keyphrase}
    placeholder = {$t('account_settings.keyphrase')}
    bind:value={$deletionForm.keyphrase}
  />
  <span slot="submitText">{$t('admin_dashboard.form_modal.delete_user')}</span>
</FormModal>
