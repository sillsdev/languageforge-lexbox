<script lang="ts">
  import { goto } from '$app/navigation';
  import { Form, Input, lexSuperForm } from '$lib/forms';
  import { SubmitButton } from '$lib/forms';
  import t from '$lib/i18n';
  import Page from '$lib/layout/Page.svelte';
  import { toSearchParams } from '$lib/util/query-params';
  import { z } from 'zod';

  const formSchema = z.object({
    email: z.string().email($t('register.email')),
  });
  let { form, errors, enhance, submitting } = lexSuperForm(formSchema, async () => {
    await fetch(`api/login/forgotPassword?${toSearchParams($form)}`, {
      method: 'POST',
    });
    await goto('/forgotPassword/emailSent');
  }, {
    taintedMessage: null,
  });
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('forgot_password.title')}
  </svelte:fragment>
  <Form {enhance}>
    <Input
      id="email"
      label={$t('register.label_email')}
      autofocus
      type="email"
      bind:value={$form.email}
      error={$errors.email}
    />
    <SubmitButton loading={$submitting}>{$t('forgot_password.send_email')}</SubmitButton>
  </Form>
</Page>
