<script lang="ts">
  import { goto } from '$app/navigation';
  import { Form, FormError, Input, SubmitButton, lexSuperForm, passwordFormRules } from '$lib/forms';
  import t from '$lib/i18n';
  import { PageBreadcrumb, TitlePage } from '$lib/layout';
  import { hash } from '$lib/util/hash';
  import { z } from 'zod';
  import { useNotifications } from '$lib/notify';
  import type { PageData } from './$types';
  import { getAspResponseErrorMessage } from '$lib/util/asp-response';
  import PasswordStrengthMeter from '$lib/components/PasswordStrengthMeter.svelte';

  export let data: PageData;

  const { notifySuccess } = useNotifications();

  const formSchema = z.object({
    password: passwordFormRules($t),
    score: z.number(),
  });
  let { form, errors, enhance, submitting, message } = lexSuperForm(formSchema, async () => {
    const response: Response = await fetch('api/login/resetPassword', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ passwordHash: await hash($form.password), passwordStrength: $form.score }),
      lexboxResponseHandlingConfig: {
        disableRedirectOnAuthError: true,
      },
    });
    if (!response.ok) {
      let errorMessage = await getAspResponseErrorMessage(response);
      return $t('reset_password.failed', { errorMessage });
    }
    notifySuccess($t('reset_password.password_reset'));
    await goto(data.home);
  });
</script>

<PageBreadcrumb href="/user">{$t('account_settings.title')}</PageBreadcrumb>

<TitlePage title={$t('reset_password.title')}>
  <Form {enhance}>
    <Input
      bind:value={$form.password}
      type="password"
      label={$t('reset_password.new_password')}
      error={$errors.password}
      autofocus
    />
    <PasswordStrengthMeter bind:score={$form.score} password={$form.password} />
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('reset_password.submit')}</SubmitButton>
  </Form>
</TitlePage>
