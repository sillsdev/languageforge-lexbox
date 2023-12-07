<script lang="ts">
  import { goto } from '$app/navigation';
  import { SubmitButton, Form, FormError, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { PageTitle } from '$lib/layout';
  import { login, logout } from '$lib/user';
  import { onMount } from 'svelte';
  import Markdown from 'svelte-exmarkdown';
  import flexLogo from '$lib/assets/flex-logo.png';
  import lfLogo from '$lib/assets/lf-logo.png';
  import oneStoryEditorLogo from '$lib/assets/onestory-editor-logo.svg';
  import weSayLogo from '$lib/assets/we-say-logo.png';
  import { z } from 'zod';

  const formSchema = z.object({
    email: z.string().min(1, $t('login.missing_user_info')),
    password: z.string().min(1, $t('login.password_missing')),
  });
  let { form, errors, message, enhance, submitting } = lexSuperForm(
    formSchema,
    async () => {
      if (await login($form.email, $form.password)) {
        await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
        return;
      }
      $message = $t('login.bad_credentials');
      badCredentials = true;
    },
    {
      taintedMessage: null,
    }
  );

  onMount(() => {
    const code = new URLSearchParams(window.location.search).get('message');
    if (code === 'link_expired') {
      $message = $t('login.link_expired');
    }
    logout();
  });
  let badCredentials = false;
</script>

<div class="hero flex-grow">
  <div class="hero-content flex-col lg:flex-row-reverse gap-16">
    <div class="prose text-lg flex-shrink-0">
      <Markdown md={$t('login.welcome')} />
      <div class="flex gap-4 not-prose justify-center">
        <a href="https://software.sil.org/fieldworks/">
          <img src={flexLogo} class="h-12" height="48" alt="FLEx Logo">
        </a>
        <a href="https://languageforge.org/">
          <img src={lfLogo} class="h-12" height="48" alt="Language Forge Logo">
        </a>
        <a href="https://software.sil.org/onestoryeditor/">
          <img src={oneStoryEditorLogo} class="h-12" height="48" alt="OneStory Editor Logo">
        </a>
        <a href="https://software.sil.org/wesay/">
          <img src={weSayLogo} class="h-12" height="48" alt="WeSay Logo">
        </a>
      </div>
    </div>
    <div class="card flex-shrink-0 w-full max-w-md shadow-2xl bg-base-200">
      <div class="card-body">
        <PageTitle title={$t('login.title')} />

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
      </div>
    </div>
  </div>
</div>
