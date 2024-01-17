<script lang="ts">
  import { goto } from '$app/navigation';
  import { Form, FormError, Input, SubmitButton, lexSuperForm, passwordFormRules } from '$lib/forms';
  import t from '$lib/i18n';
  import { TitlePage } from '$lib/layout';
  import { hash } from '$lib/util/hash';
  import { z } from 'zod';
  import { useNotifications } from '$lib/notify';
  import type { PageData } from './$types';

  export let data: PageData;

  const { notifySuccess } = useNotifications();

  const formSchema = z.object({
    password: passwordFormRules($t),
  });
  let { form, errors, enhance, submitting, message } = lexSuperForm(formSchema, async () => {
    const response = await fetch('api/login/resetPassword', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ passwordHash: await hash($form.password) }),
      lexboxResponseHandlingConfig: {
        disableRedirectOnAuthError: true,
      },
    });
    if (!response.ok) {
      return $t('reset_password.failed', {statusText: response.statusText});
    }
    notifySuccess($t('reset_password.password_reset'));
    await goto(data.home);
  });
</script>

<TitlePage title={$t('reset_password.title')}>
  <Form {enhance}>
    <Input
      bind:value={$form.password}
      type="password"
      label={$t('reset_password.new_password')}
      error={$errors.password}
      autofocus
    />
    <FormError error={$message} />
    <SubmitButton loading={$submitting}>{$t('reset_password.submit')}</SubmitButton>
  </Form>
</TitlePage>
