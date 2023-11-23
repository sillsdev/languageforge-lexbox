<script lang="ts">
  import { SubmitButton, FormError, Input, ProtectedForm, lexSuperForm } from '$lib/forms';
  import { passwordFormRules } from '$lib/forms/utils';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import { goHome, register } from '$lib/user';
  import { z } from 'zod';

  let turnstileToken = '';
  const formSchema = z.object({
    name: z.string().min(1, $t('register.name_missing')),
    email: z.string().email($t('register.email')),
    password: passwordFormRules($t),
  });

  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const { user, error } = await register($form.password, $form.name, $form.email, turnstileToken);
    if (error) {
      if (error.turnstile) {
        $message = $t('turnstile.invalid');
      }
      if (error.accountExists) {
        $errors.email = [$t('register.account_exists')];
      }
      return;
    }
    if (user) {
      await goHome();
      return;
    }
    throw new Error('Unknown error, no error from server, but also no user.');
  });
</script>

<Page>
  <svelte:fragment slot="header">{$t('register.title')}</svelte:fragment>

  <ProtectedForm {enhance} bind:turnstileToken>
    <Input autofocus id="name" label={$t('register.label_name')} bind:value={$form.name} error={$errors.name} />
    <Input
      id="email"
      label={$t('register.label_email')}
      description={$t('register.description_email')}
      type="email"
      bind:value={$form.email}
      error={$errors.email}
    />
    <Input
      id="password"
      label={$t('register.label_password')}
      type="password"
      bind:value={$form.password}
      error={$errors.password}
      autocomplete="new-password"
    />

    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('register.button_register')}</SubmitButton>
  </ProtectedForm>
</Page>
