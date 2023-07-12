<script lang="ts">
  import { goto } from '$app/navigation';
  import { Button, Form, Input, lexSuperForm } from '$lib/forms';
  import t from '$lib/i18n';
  import Page from '$lib/layout/Page.svelte';
  import { hash } from '$lib/user';
  import { z } from 'zod';
  import {notifySuccess} from '$lib/notify/';
  import AddProjectMember from '../project/[project_code]/AddProjectMember.svelte';

  const formSchema = z.object({
    password: z.string().min(4, $t('login.password_missing')),
  });
  let { form, errors, enhance, submitting } = lexSuperForm(formSchema, async () => {
    await fetch('api/login/resetPassword', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ passwordHash: await hash($form.password) }),
    });
    notifySuccess($t('login.password_reset'));
    await goto('/');
  });
</script>

<Page>
<svelte:fragment slot="header">
    {$t('reset_password.title')}
</svelte:fragment>
  <Form {enhance}>
    <Input
      bind:value={$form.password}
      type="password"
      label={$t('reset_password.new_password')}
      error={$errors.password}
      autofocus
    />
    <Button loading={$submitting}>{$t('reset_password.submit')}</Button>
  </Form>
</Page>
