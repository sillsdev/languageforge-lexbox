<script lang="ts">
  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import { Button, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import { Page } from '$lib/layout';
  import { _changeUserAccountData } from './+page';
  import type { ChangeUserAccountDataInput } from '$lib/gql/types';
  import { notifySuccess } from '$lib/notify';
  import z from 'zod';

  export let data: PageData;
  const user = data.user;

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

  let sendingVerificationEmail = false;
  let sentVerificationEmail = false;

  async function sendVerificationEmail(): Promise<void> {
    sendingVerificationEmail = true;
    const result = await fetch('/api/user/sendVerificationEmail', { method: 'POST' });
    if (!result.ok) throw Error(`Failed to send verification email. ${result.status}: ${result.statusText}.`);
    sentVerificationEmail = true;
    sendingVerificationEmail = false;
  }
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('account_settings.title')}
  </svelte:fragment>
  {#if data.verifiedEmail}
    <div class="alert alert-success mb-4">
      <span>{$t('account_settings.verify_email.success')}</span>
      <span class="i-mdi-check-circle-outline text-5xl" />
      <a class="btn" href="/">{$t('account_settings.verify_email.go_to_projects')}</a>
    </div>
  {:else if !user?.emailVerified}
    {#if sentVerificationEmail}
      <div class="alert alert-info mb-4">
        <span>{$t('account_settings.verify_email.check_inbox')}</span>
        <span class="i-mdi-email-heart-outline text-5xl" />
      </div>
    {:else}
      <div class="alert alert-warning mb-4">
        <span>{$t('account_settings.verify_email.please_verify')}</span>
        <button class="btn" class:loading={sendingVerificationEmail} on:click={sendVerificationEmail}>
          {$t('account_settings.verify_email.resend')}</button
        >
      </div>
    {/if}
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
    <Input id="email" label={$t('account_settings.email')} type="email" error={$errors.email} bind:value={$form.email} />
    <a class="link my-4" href="/resetPassword">
      {$t('account_settings.reset_password')}
    </a>
    <FormError error={$message} />
    <Button loading={$submitting}>{$t('account_settings.button_update')}</Button>
  </Form>
</Page>
