<script lang="ts">
  import { goto } from '$app/navigation';
  import { SubmitButton, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { login, logout } from '$lib/user';
  import { onMount } from 'svelte';
  import { z } from 'zod';

  const formSchema = z.object({
    email: z.string().min(1, $t('login.missing_user_info')),
    password: z.string().min(1, $t('login.password_missing')),
  });
  let { form, errors, message, enhance, submitting } = lexSuperForm(
    formSchema,
    async () => {
      if (await login($form.email, $form.password)) {
        await goto('/home');
        return;
      }
      $message = $t('login.bad_credentials');
      badCredentials = true;
    },
    {
      taintedMessage: null,
    },
  );

  onMount(logout);
  let badCredentials = false;
</script>

<Page>
  <svelte:fragment slot="header">{$t('login.title')}</svelte:fragment>

  <Form {enhance}>
    <Input
      id="email"
      label={$t('login.label_email')}
      type="text"
      bind:value={$form.email}
      error={$errors.email}
      autofocus
    />

    <Input
      id="password"
      label={$t('login.label_password')}
      type="password"
      bind:value={$form.password}
      error={$errors.password}
    />

    <FormError error={$message} />

    <a class="link mt-0" href="/forgotPassword">
      {$t('login.forgot_password')}
    </a>

    {#if badCredentials}
      <SubmitButton loading={$submitting}>{$t('login.button_login_again')}</SubmitButton>
    {:else}
      <SubmitButton loading={$submitting}>{$t('login.button_login')}</SubmitButton>
    {/if}
    <a class="btn btn-primary" href="/register">{$t('register.title')}</a>
  </Form>
</Page>
