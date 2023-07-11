<script lang="ts">
  import { Button, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { notifySuccess } from '$lib/notify';
  import { slide } from 'svelte/transition';
  import z from 'zod';
  import type { PageData } from './$types';
  import { _changeUserAccountData } from './+page';

  export let data: PageData;
  const user = data.user;

  const formSchema = z.object({
    email: z.string().email(),
    name: z.string(),
  });

  let newEmail: string | undefined;

  let { form, errors, enhance, message, submitting } = lexSuperForm(formSchema, async () => {
    const formEmail = $form.email; // the next line invalidate everything and resets the form
    const result = await _changeUserAccountData({
      email: $form.email,
      name: $form.name,
      userId: user.id,
    });
    if (result.error) {
      $message = result.error.message;
      return;
    }

    if (formEmail !== user.email) {
      newEmail = formEmail;
    }

    notifySuccess('Your account has been updated.');
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
  {#if data.verifiedEmail || data.changedEmail}
    <div class="alert alert-success" transition:slide|local>
      {#if data.verifiedEmail}
        <span>{$t('account_settings.verify_email.verify_success')}</span>
      {:else}
        <span>{$t('account_settings.verify_email.change_success')}</span>
      {/if}
      <span class="i-mdi-check-circle-outline" />
      <a class="btn" href="/">{$t('account_settings.verify_email.go_to_projects')}</a>
    </div>
  {:else if newEmail}
    <div class="alert alert-info" transition:slide|local>
      <div>
        <span>{$t('account_settings.verify_email.you_have_mail')}</span>
        <span>{$t('account_settings.verify_email.verify_to_change', { newEmail })}</span>
      </div>
      <span class="i-mdi-email-heart-outline" />
    </div>
  {:else if !user?.emailVerified}
    {#if sentVerificationEmail}
      <div class="alert alert-info" transition:slide|local>
        <div>
          <span>{$t('account_settings.verify_email.you_have_mail')}</span>
          <span>{$t('account_settings.verify_email.check_inbox')}</span>
        </div>
        <span class="i-mdi-email-heart-outline" />
      </div>
    {:else}
      <div class="alert alert-warning" transition:slide|local>
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

<style lang="postcss">
  .alert {
    @apply mb-4;

    & > span[class*='i-mdi'] {
      flex: 0 0 40px;
      @apply text-5xl;
    }

    & > div {
      @apply flex-col items-start;
      & > span:first-child {
        @apply font-bold;
      }
    }
  }
</style>
