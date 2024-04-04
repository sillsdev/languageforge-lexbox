<script lang="ts">
  import { useEmailResult, useRequestedEmail } from '$lib/email/EmailVerificationStatus.svelte';
  import { DisplayLanguageSelect, Form, FormError, Input, SubmitButton, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { _changeUserAccountData } from './+page';
  import { useNotifications } from '$lib/notify';
  import z from 'zod';
  import { goto } from '$app/navigation';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import type { PageData } from './$types';
  import { TrashIcon } from '$lib/icons';
  import { onMount } from 'svelte';
  import { DialogResponse } from '$lib/components/modals';
  import MoreSettings from '$lib/components/MoreSettings.svelte';
  import { delay } from '$lib/util/time';

  export let data: PageData;
  $: user = data.account;
  let deleteModal: DeleteUserModal;

  const emailResult = useEmailResult();
  const requestedEmail = useRequestedEmail();
  $: if (data.emailResult) emailResult.set(data.emailResult);

  const { notifySuccess, notifyWarning } = useNotifications();

  async function openDeleteModal(): Promise<void> {
    let { response } = await deleteModal.open($user);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('account_settings.delete_success'));
      await delay();
      await goto('/logout');
    }
  }

  const formSchema = z.object({
    email: z.string().email($t('form.invalid_email')).nullish(),
    name: z.string(),
    locale: z.string().min(2),
  });

  let { form, errors, enhance, message, submitting, formState } = lexSuperForm(formSchema, async () => {
    const { error, data } = await _changeUserAccountData({
      email: $form.email,
      name: $form.name,
      locale: $form.locale,
      userId: $user.id,
    });
    if (data?.changeUserAccountBySelf.errors?.some(e => e.__typename === 'UniqueValueError')) {
      $errors.email = [$t('account_settings.email_taken')];
      return;
    }
    if (error?.message) {
      return error.message;
    }

    if ($formState.email.changed && $form.email) {
      requestedEmail.set($form.email);
    }

    if ($formState.name.tainted || $formState.locale.tainted) {
      notifySuccess($t('account_settings.update_success'));
    }
  });

  // This is a bit of a hack to make sure that the email field is not required if the user has no email
  // even if the user edited the email field
  $: if(!$form.email && $user && !$user.email) $form.email = null;

  onMount(() => {
    form.set(
      {
        email: $user.email ?? null,
        name: $user.name,
        locale: $user.locale,
      },
      { taint: false }
    );
  });
</script>

<TitlePage title={$t('account_settings.title')}>
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
    <DisplayLanguageSelect
      bind:value={$form.locale}
    />
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('account_settings.button_update')}</SubmitButton>
  </Form>
  <div class="mt-4">
    <a class="link" href="/resetPassword">
      {$t('account_settings.reset_password')}
    </a>
  </div>
  <MoreSettings>
    <button class="btn btn-error" on:click={openDeleteModal}>
      {$t('account_settings.delete_account.submit')}<TrashIcon />
    </button>
  </MoreSettings>
</TitlePage>
<DeleteUserModal bind:this={deleteModal} i18nScope="account_settings.delete_account" />
