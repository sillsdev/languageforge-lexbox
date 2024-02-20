<script lang="ts">
  import { env } from '$env/dynamic/public';
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
  import { navigating } from '$app/stores';
  import { AUTHENTICATED_ROOT } from '../..';

  const googleClientId = env.PUBLIC_GOOGLE_OAUTH_CLIENT_ID;
  const formSchema = z.object({
    email: z.string().min(1, $t('login.missing_user_info')),
    password: z.string().min(1, $t('login.password_missing')),
  });
  let { form, errors, message, enhance, submitting } = lexSuperForm(
    formSchema,
    async () => {
      errors.clear();
      badCredentials = false;

      const loginResult = await login($form.email, $form.password);

      if (loginResult.success) {
        await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
      } else if (loginResult.error === 'Locked') {
        $message = $t('login.your_account_is_locked');
      } else {
        $message = $t('login.bad_credentials');
        badCredentials = true;
      }
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

<svelte:head>
  <script src="https://accounts.google.com/gsi/client" async></script>
</svelte:head>

<div class="hero flex-grow">
  <div class="grid lg:grid-cols-2 gap-8 place-items-center">
    <div class="card w-full max-w-md sm:shadow-2xl sm:bg-base-200">
      <div class="card-body sm-only:p-0">
        <PageTitle title={$t('login.title')} />

        <!-- Google signin code looks for these two div IDs in your page -->
        <!-- NOTE: Valid values for data-context also include "use" or "signup" -->
        <div id="g_id_onload"
          data-client_id={googleClientId}
          data-context="signin"
          data-ux_mode="popup"
          data-login_uri="/api/login/signin-google"
          data-auto_prompt="false">  <!-- If true, this will pop up a login suggestion in upper right corner of page -->
        </div>

        <div class="g_id_signin"
          data-type="standard"
          data-shape="rectangular"
          data-theme="filled_blue"
          data-text="signin_with"
          data-size="large"
          data-logo_alignment="left">
        </div>


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
            autocomplete="current-password"
          />

          <div class="markdown-wrapper">
            <FormError error={$message} markdown />
          </div>

          <a class="link mt-0" href="/forgotPassword">
            {$t('login.forgot_password')}
          </a>

          <SubmitButton loading={$submitting || $navigating?.to?.route.id?.includes(AUTHENTICATED_ROOT)}>
            {badCredentials ? $t('login.button_login_again') : $t('login.button_login')}
          </SubmitButton>

          <a class="btn btn-primary" href="/register">{$t('register.title')}</a>
        </Form>
      </div>
    </div>

    <div class="flex flex-col gap-8 lg:flex-col-reverse">
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

      <div class="prose text-lg">
        <Markdown md={$t('login.welcome_header')} />
        <Markdown md={$t('login.welcome')} />
      </div>
    </div>
  </div>
</div>

<style lang="postcss">
  :global(.markdown-wrapper a) {
    @apply link link-hover text-base-content;
  }

  :global(.markdown-wrapper .label) {
    @apply p-0 mb-2;
  }
</style>
