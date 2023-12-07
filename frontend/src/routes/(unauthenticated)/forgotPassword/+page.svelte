<script lang="ts">
  import { goto } from '$app/navigation';
  import { ProtectedForm, Input, lexSuperForm, FormError } from '$lib/forms';
  import { SubmitButton } from '$lib/forms';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { z } from 'zod';

  type ForgotPasswordResponseErrors = {
    errors: {
      /* eslint-disable @typescript-eslint/naming-convention */
      TurnstileToken?: unknown,
      /* eslint-enable @typescript-eslint/naming-convention */
    }
  }

  const formSchema = z.object({
    email: z.string().email($t('register.email')),
  });

  let turnstileToken = '';

  let { form, errors, enhance, submitting, message } = lexSuperForm(formSchema, async () => {
    const response = await fetch(`api/login/forgotPassword`, {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
      },
      body: JSON.stringify({
        ...$form,
        turnstileToken,
      }),
    });

    if (!response.ok) {
      const { errors } = await response.json() as ForgotPasswordResponseErrors;
      const turnstileError = !!errors.TurnstileToken;
      if (!turnstileError) throw new Error('Unknown error', { cause: errors });
      $message = $t('turnstile.invalid');
      return;
    }

    await goto('/forgotPassword/emailSent');
  }, {
    taintedMessage: null,
  });
</script>

<TitlePage title={$t('forgot_password.title')}>
  <ProtectedForm {enhance} bind:turnstileToken>
    <Input
      id="email"
      label={$t('register.label_email')}
      autofocus
      type="email"
      bind:value={$form.email}
      error={$errors.email}
    />
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('forgot_password.send_email')}</SubmitButton>
  </ProtectedForm>
</TitlePage>
