<script lang="ts">
  import { goto } from '$app/navigation';
  import { Form, Input, lexSuperForm } from '$lib/forms';
  import Button from '$lib/forms/Button.svelte';
  import t from '$lib/i18n';
  import Page from '$lib/layout/Page.svelte';
  import { z } from 'zod';

  const formSchema = z.object({
    email: z.string().email($t('register.email')),
  });
  let { form, errors, enhance, submitting } = lexSuperForm(formSchema, async () => {
    await fetch(`api/login/forgotPassword?email=${$form.email}`, {
      method: 'POST',
    });
    await goto('/forgotPassword/emailSent');
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
    <Button loading={$submitting}>{$t('forgot_password.send_email')}</Button>
  </Form>
</Page>
