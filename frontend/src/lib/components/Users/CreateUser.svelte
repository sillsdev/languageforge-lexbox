<script lang="ts">
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';
  import {
    SubmitButton,
    FormError,
    Input,
    MaybeProtectedForm,
    isEmail,
    lexSuperForm,
    passwordFormRules,
    DisplayLanguageSelect,
  } from '$lib/forms';
  import t, {getLanguageCodeFromNavigator, locale} from '$lib/i18n';
  import {type LexAuthUser, type RegisterResponse} from '$lib/user';
  import {getSearchParamValues} from '$lib/util/query-params';
  import {onMount} from 'svelte';
  import {usernameRe} from '$lib/user';
  import {z} from 'zod';
  import type {StringifyValues} from '$lib/type.utils';

  interface Props {
    allowUsernames?: boolean;
    errorOnChangingEmail?: string;
    skipTurnstile?: boolean;
    submitButtonText?: string;
    handleSubmit: (
      password: string,
      passwordStrength: number,
      name: string,
      email: string,
      locale: string,
      turnstileToken: string,
    ) => Promise<RegisterResponse>;
    onSubmitted?: (submittedUser: LexAuthUser) => void;
    formTainted?: boolean;
  }

  let {
    allowUsernames = false,
    errorOnChangingEmail = '',
    skipTurnstile = false,
    submitButtonText = $t('register.button_register'),
    handleSubmit,
    onSubmitted,
    formTainted = $bindable(false),
  }: Props = $props();

  type RegisterPageQueryParams = {
    name: string;
    email: string;
  };
  let turnstileToken = $state('');
  let urlValues = {} as StringifyValues<RegisterPageQueryParams>;

  function validateAsEmail(value: string): boolean {
    return !allowUsernames || value.includes('@');
  }

  // $locale is the locale that our i18n is using for them (i.e. the best available option we have for them)
  // getLanguageCodeFromNavigator() gives us the language/locale they probably actually want. Maybe we'll support it in the future.
  const userLocale = getLanguageCodeFromNavigator() ?? $locale;
  const formSchema = z.object({
    name: z.string().trim().min(1, $t('register.name_missing')
  ),
    email: z
      .string()
      .trim()
      .min(1, $t('project_page.add_user.empty_user_field'))
      .refine((value) => !errorOnChangingEmail || !urlValues.email || value == urlValues.email, { error: (() => errorOnChangingEmail)() })
      .refine((value) => !validateAsEmail(value) || isEmail(value), { error: $t('form.invalid_email') })
      .refine((value) => validateAsEmail(value) || usernameRe.test(value), { error: $t('register.invalid_username') }),
    password: passwordFormRules($t),
    score: z.number(),
    locale: z.string().trim().min(2).default(userLocale),
  });

  let { form, errors, message, enhance, submitting, tainted } = lexSuperForm(formSchema, async () => {
    const { user, error } = await handleSubmit(
      $form.password,
      $form.score,
      $form.name,
      $form.email,
      $form.locale,
      turnstileToken,
    );
    if (error) {
      if (error.turnstile) {
        $message = $t('turnstile.invalid');
      }
      if (error.accountExists) {
        $errors.email = [
          validateAsEmail($form.email) ? $t('register.account_exists_email') : $t('register.account_exists_login'),
        ];
      }
      if (error.invalidInput) {
        $errors.email = [validateAsEmail($form.email) ? $t('form.invalid_email') : $t('register.invalid_username')];
      }
      return;
    }
    if (user) {
      onSubmitted?.(user);
      return;
    }
    throw new Error('Unknown error, no error from server, but also no user.');
  }, {
    resetForm: false,
    taintedMessage: true,
  });

  $effect(() => {
    formTainted = !!$tainted;
  });
  onMount(() => {
    // query params not available during SSR
    urlValues = getSearchParamValues<RegisterPageQueryParams>();
    form.update(
      (form) => {
        if (urlValues.name) form.name = urlValues.name;
        if (urlValues.email) form.email = urlValues.email;
        return form;
      },
      { taint: true },
    );
  });
</script>

<MaybeProtectedForm {skipTurnstile} {enhance} bind:turnstileToken>
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
  <PasswordStrengthMeter onScoreUpdated={(score) => ($form.score = score)} password={$form.password} />
  <DisplayLanguageSelect bind:value={$form.locale} />
  <FormError error={$message} />
  <SubmitButton loading={$submitting}>{submitButtonText}</SubmitButton>
</MaybeProtectedForm>

<style lang="postcss">
  .email :global(.description) {
    @apply text-success;
  }
</style>
