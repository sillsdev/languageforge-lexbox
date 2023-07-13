<script lang="ts">
  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import { Button, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import { Page } from '$lib/layout';
  import { _changeUserAccountData } from './+page';
  import type { ChangeUserAccountDataInput } from '$lib/gql/types';
<<<<<<< HEAD
  import { TrashIcon } from '$lib/icons';
=======
  import { notifySuccess } from '$lib/notify';
>>>>>>> 300915e5a9acb184f1b289fcc2f3d9a86002a2db
  import z from 'zod';
  import { goto } from '$app/navigation';
  import DeleteAccountModal from './DeleteAccountModal.svelte';
  import { _deleteUserByUser } from './+page';
  import type { DeleteUserByUserInput } from '$lib/gql/types';

  export let data: PageData;
<<<<<<< HEAD
  $: user = data?.user;
  $: userid = user?.id;
  let deleteModal: DeleteAccountModal;
=======
  const user = data.user;
>>>>>>> 300915e5a9acb184f1b289fcc2f3d9a86002a2db

  async function openDeleteModal(): Promise<void> {
    await deleteModal.open();
  }
  const formSchema = z.object({
    email: z.string().email(),
    name: z.string(),
  });
  async function deleteMe(): Promise<void> {
    const deleteUserByUserInput: DeleteUserByUserInput = {
      userId: userid as string,
    };
    await _deleteUserByUser(deleteUserByUserInput);
    await goto('/logout');
  }
  let { form, errors, enhance, message, submitting } = lexSuperForm(formSchema, async () => {
    const changeUserAccountDataInput: ChangeUserAccountDataInput = {
      email: $form.email,
      name: $form.name,
      userId: user.id,
    };
    const result = await _changeUserAccountData(changeUserAccountDataInput);
    $message = result.error?.message;
    if (!$message) {
      notifySuccess('Your account has been updated.');
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
<DeleteAccountModal deleteUser={deleteMe} bind:this={deleteModal} />
