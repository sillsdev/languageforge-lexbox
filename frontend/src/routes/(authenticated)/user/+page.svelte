<script lang="ts">
  import { emailResult, requestedEmail } from '$lib/email/EmailVerificationStatus.svelte';
  import { SubmitButton, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { _changeUserAccountData } from './+page';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import z from 'zod';
  import { goto } from '$app/navigation';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import type { PageData } from './$types';
  import { TrashIcon } from '$lib/icons';
  import { onMount } from 'svelte';
  import { DialogResponse } from '$lib/components/modals';

  export let data: PageData;
  $: user = data?.user;
  $: userid = user?.id;
  let deleteModal: DeleteUserModal;

  $: if (data.emailResult) emailResult.set(data.emailResult);

  async function openDeleteModal(): Promise<void> {
    let { response } = await deleteModal.open(userid);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('account_settings.delete_success'));
      await goto('/logout');
    }
  }
  const formSchema = z.object({
    email: z.string().email(),
    name: z.string(),
  });

  let { form, errors, enhance, message, submitting, formState } = lexSuperForm(formSchema, async () => {
    const { error } = await _changeUserAccountData({
      email: $form.email,
      name: $form.name,
      userId: user.id,
    });

    if (error?.message) {
      return error.message;
    }

    if ($formState.email.changed) {
      requestedEmail.set($form.email);
    }

    if ($formState.name.tainted) {
      notifySuccess($t('account_settings.update_success'));
    }
  });
  onMount(() => {
    form.set(
      {
        email: user.email,
        name: user.name,
      },
      { taint: false }
    );
  });
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('account_settings.title')}
  </svelte:fragment>
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
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('account_settings.button_update')}</SubmitButton>
  </Form>
  <div class="mt-4">
    <a class="link" href="/resetPassword">
      {$t('account_settings.reset_password')}
    </a>
  </div>
  <div class="collapse border border-neutral-content collapse-arrow mt-4">
    <input type="checkbox" />
    <div class="collapse-title text-lg">{$t('account_settings.more_settings')}</div>
    <div class="collapse-content">
      <div class="divider mt-0" />
      <div class="flex justify-end">
        <button class="btn btn-error" on:click={openDeleteModal}
          >{$t('account_settings.delete_account')}<TrashIcon /></button
        >
      </div>
    </div>
  </div>
</Page>
<DeleteUserModal bind:this={deleteModal} />
