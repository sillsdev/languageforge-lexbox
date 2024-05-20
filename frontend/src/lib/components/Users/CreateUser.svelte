<script lang="ts">
  import { goto } from '$app/navigation';
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';
  import { SubmitButton, FormError, Input, ProtectedForm, isEmail, lexSuperForm, passwordFormRules, DisplayLanguageSelect } from '$lib/forms';
  import t, { getLanguageCodeFromNavigator, locale } from '$lib/i18n';
  import { register, acceptInvitation, createGuestUserByAdmin } from '$lib/user';
  import { getSearchParamValues } from '$lib/util/query-params';
  import { createEventDispatcher, onMount } from 'svelte';
  import { usernameRe } from '$lib/user';
  import { z } from 'zod';

  export let allowUsernames = false;
  export let submitButtonText = $t('register.button_register');
  export let endpoint: 'register' | 'acceptInvitation' | 'createGuestUserByAdmin';

  const dispatch = createEventDispatcher();

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
    email: z.string().trim()
      .min(1, $t('project_page.add_user.empty_user_field'))
      .refine((value) => isEmail(value) || (allowUsernames && usernameRe.test(value)), { message: $t('register.invalid_input') }),
    password: passwordFormRules($t),
    score: z.number(),
    locale: z.string().trim().min(2).default(userLocale),
  });

  let { form, errors, message, enhance, submitting } = lexSuperForm(formSchema, async () => {
    const endpointHandler =
        endpoint === 'acceptInvitation' ? acceptInvitation
      : endpoint === 'register' ? register
      : endpoint === 'createGuestUserByAdmin' ? createGuestUserByAdmin
      : () => { throw new Error(`CreateUser doesn't know how to handle endpoint type ${endpoint}`) };
    const { user, error } = await endpointHandler($form.password, $form.score, $form.name, $form.email, $form.locale, turnstileToken);
    if (error) {
      if (error.turnstile) {
        $message = $t('turnstile.invalid');
      }
      if (error.accountExists) {
        $errors.email = [$t('register.account_exists')];
      }
      if (error.invalidInput) {
        $errors.email = [$t('register.invalid_input')];
      }
      return;
    }
    if (user) {
      dispatch('submitted');
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
      label={$t(allowUsernames ? 'register.label_email_or_username' : 'register.label_email')}
      description={$t('register.description_email')}
      type={allowUsernames ? 'text' : 'email'}
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
  <SubmitButton loading={$submitting}>{submitButtonText}</SubmitButton>
</ProtectedForm>

<style lang="postcss">
  .email :global(.description) {
    @apply text-success;
  }
</style>
