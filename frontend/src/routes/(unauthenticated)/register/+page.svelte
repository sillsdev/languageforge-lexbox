<script lang="ts">
  import { goto } from '$app/navigation';
  import { Button, Input, ProtectedForm, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { register } from '$lib/user';
  import { z } from 'zod';

  const formSchema = z.object({
    name: z.string().min(1, $t('register.name_missing')),
    email: z.string().email($t('register.email')),
    password: z.string().min(1, $t('register.password_missing')),
  });
  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const { user, error } = await register($form.password, $form.name, $form.email, turnstileToken);
    if (error) {
      if (error.turnstile) {
        $message = $t('register.turnstile_error');
      }
      if (error.accountExists) {
        $errors.email = [$t('register.account_exists')];
      }
      return;
    }
    if (user) {
      await goto('/');
      return;
    }
    throw new Error('Unknown error, no error from server, but also no user.');
  });
  let turnstileToken = '';
</script>

<Page>
  <svelte:fragment slot="header">{$t('register.title')}</svelte:fragment>

  <ProtectedForm {enhance} bind:turnstileToken>
    <Input id="name" label={$t('register.label_name')} required bind:value={$form.name} error={$errors.name} />
    <Input
      id="email"
      label={$t('register.label_email')}
      type="email"
      required
      bind:value={$form.email}
      error={$errors.email}
    />
    <Input
      id="password"
      label={$t('register.label_password')}
      type="password"
      required
      bind:value={$form.password}
      error={$errors.password}
    />
    {#if $message}
      <aside class="alert alert-error">
        {$message}
      </aside>
    {/if}
    <Button loading={$submitting}>{$t('register.button_register')}</Button>
  </ProtectedForm>
</Page>
