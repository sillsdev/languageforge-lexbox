<script lang="ts">
  import EmailVerificationStatus from '$lib/email/EmailVerificationStatus.svelte';
  import { Button, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { notifySuccess } from '$lib/notify';
  import z from 'zod';
  import type { PageData } from './$types';
  import { _changeUserAccountData } from './+page';

  export let data: PageData;
  const user = data.user;

  const formSchema = z.object({
    email: z.string().email(),
    name: z.string(),
  });

  let requestedEmail: string | undefined;

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
      requestedEmail = $form.email;
    }

    if ($formState.name.tainted) {
      notifySuccess($t('account_settings.update_success'));
    }
  });

  form.set(
    {
      email: user.email,
      name: user.name,
    },
    { taint: false }
  );
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('account_settings.title')}
  </svelte:fragment>
  {#if user.emailVerified}
    <div class="mb-4">
      <EmailVerificationStatus {user} emailResult={data.emailResult} {requestedEmail} />
    </div>
  {/if}
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
    <FormError error={$message} />
    <Button loading={$submitting}>{$t('account_settings.button_update')}</Button>
  </Form>
</Page>
