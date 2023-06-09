<script lang="ts">
  import { goto } from '$app/navigation';
  import { Button, Form, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { login, logout, user } from '$lib/user';
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
        await goto($user?.role == 'admin' ? '/admin' : '/');
        return;
      }
      $message = $t('login.bad_credentials');
      badCredentials = true;
    },
    {
      taintedMessage: false,
      clearOnSubmit: 'errors',
    }
  );

  onMount(logout);
  let badCredentials = false;
</script>

<Page>
  <svelte:fragment slot="header">{$t('login.page_header')}</svelte:fragment>

  <Form {enhance}>
    <Input
      id="email"
      label={$t('login.label_email')}
      type="text"
      bind:value={$form.email}
      error={$errors.email}
      autofocus
      required
    />

    <Input
      id="password"
      label={$t('login.label_password')}
      type="password"
      bind:value={$form.password}
      error={$errors.password}
      required
    />
    <a class="link mt-0" href="/forgotPassword">
      {$t('login.forgot-password')}
    </a>

    {#if badCredentials}
      <aside hidden={!!$message} class="alert alert-error">
        {$message}
      </aside>

      <Button loading={$submitting}>{$t('login.button_login_again')}</Button>
    {:else}
      <Button loading={$submitting}>{$t('login.button_login')}</Button>
    {/if}
    <a class="btn btn-primary" href="/register">Register</a>
  </Form>
</Page>
