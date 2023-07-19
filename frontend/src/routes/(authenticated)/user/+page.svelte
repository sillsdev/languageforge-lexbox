<script lang="ts">
  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import { Button, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import { Page } from '$lib/layout';
  import { _changeUserAccountData } from './+page';
  import type { ChangeUserAccountDataInput } from '$lib/gql/types';
  import { TrashIcon } from '$lib/icons';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import z from 'zod';
  import { goto } from '$app/navigation';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';

  export let data: PageData;
  $: user = data?.user;
  $: userid = user?.id;
  let deleteModal: DeleteUserModal;


  async function openDeleteModal(): Promise<void> {
    await deleteModal.open(userid);
    notifyWarning($t('account_settings.delete_success'))
    await goto('/logout');
  }
  const formSchema = z.object({
    email: z.string().email(),
    name: z.string(),
  });
  let { form, errors, enhance, message, submitting } = lexSuperForm(formSchema, async () => {
    const changeUserAccountDataInput: ChangeUserAccountDataInput = {
      email: $form.email,
      name: $form.name,
      userId: user.id,
    };
    const result = await _changeUserAccountData(changeUserAccountDataInput);
    $message = result.error?.message;
    if (!$message) {
      notifySuccess($t('account_settings.success'));
    }
  });
  $: {
    form.set(
      {
        email: user.email,
        name: user.name,
      },
      { taint: false }
    );
  }
</script>

<Page>
  <div class="content-center">
    <Form {enhance}>
      <Input
        id="name"
        label={$t('account_settings.name')}
        type="text"
        error={$errors.name}
        bind:value={$form.name}
        autofocus
      />
      <Input
        id="email"
        label={$t('account_settings.email')}
        type="email"
        error={$errors.email}
        bind:value={$form.email}
      />
      <a class="link my-4" href="/resetPassword">
        {$t('account_settings.reset_password')}
      </a>
      <div class="collapse text-error w-full underline rounded-box collapse-arrow">
        <input type="checkbox" />
        <div class="collapse-title text-xl font-medium">{$t('account_settings.more_settings')}</div>
        <div class="collapse-content">
          <span class="btn btn-error" on:click={openDeleteModal}>{$t('account_settings.delete_account')}<TrashIcon/></span>
        </div>
      </div>
      <FormError error={$message} />
      <Button loading={$submitting}>{$t('account_settings.button_update')}</Button>
    </Form>
  </div>
</Page>
<DeleteUserModal bind:this={deleteModal} />
