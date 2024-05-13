<script lang="ts">
  import { goto } from '$app/navigation';
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';
  import { SubmitButton, FormError, Input, ProtectedForm, lexSuperForm, passwordFormRules, DisplayLanguageSelect } from '$lib/forms';
  import t, { getLanguageCodeFromNavigator, locale } from '$lib/i18n';
  import { register } from '$lib/user';
  import { getSearchParamValues } from '$lib/util/query-params';
  import { onMount } from 'svelte';
  import { z } from 'zod';

  export let autoLogin = true;
  export let onSubmit: (() => void) | undefined = undefined;

  type RegisterPageQueryParams = {
    name: string;
    email: string;
  };
  let turnstileToken = '';
  // $locale is the locale that our i18n is using for them (i.e. the best available option we have for them)
  // getLanguageCodeFromNavigator() gives us the language/locale they probably actually want. Maybe we'll support it in the future.
  const userLocale = getLanguageCodeFromNavigator() ?? $locale;
  const formSchema = z.object({
    name: z.string().trim().min(1, $t('register.name_missing')),
    email: z.string().email($t('form.invalid_email')),
    password: passwordFormRules($t),
    score: z.number(),
    locale: z.string().trim().min(2).default(userLocale),
  });

  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const { user, error } = await register($form.password, $form.score, $form.name, $form.email, $form.locale, turnstileToken, autoLogin);
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
      if (onSubmit) onSubmit();
      if (autoLogin) await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
      return;
    }
    throw new Error('Unknown error, no error from server, but also no user.');
  });
  onMount(() => { // query params not available during SSR
    const urlValues = getSearchParamValues<RegisterPageQueryParams>();
    form.update((form) => {
      if (urlValues.name) form.name = urlValues.name;
      if (urlValues.email) form.email = urlValues.email;
      return form;
    }, { taint: true });
  });
</script>

<ProtectedForm {enhance} bind:turnstileToken>
  <Input autofocus id="name" label={$t('register.label_name')} bind:value={$form.name} error={$errors.name} />
  <div class="contents email">
    <Input
      id="email"
      label={$t('register.label_email')}
      description={$t('register.description_email')}
      type="email"
      bind:value={$form.email}
      error={$errors.email}
    />
  </div>
  <Input
    id="password"
    label={$t('register.label_password')}
    type="password"
    bind:value={$form.password}
    error={$errors.password}
    autocomplete="new-password"
  />
  <PasswordStrengthMeter bind:score={$form.score} password={$form.password} />
  <DisplayLanguageSelect
    bind:value={$form.locale}
  />
  <FormError error={$message} />
  <SubmitButton loading={$submitting}>{$t('register.button_register')}</SubmitButton>
</ProtectedForm>

<style lang="postcss">
  .email :global(.description) {
    @apply text-success;
  }
</style>
