<script lang="ts">
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import { Button, Form, Input, lexSuperForm } from '$lib/forms';
    import { Page } from '$lib/layout';
    import EditableText from '$lib/components/EditableText.svelte';
    import { z } from 'zod';

    const formSchema = z.object({ //not entirely sure what this is
    email: z.string().min(1, $t('account_settings.missing_user_info')),
    password: z.string().min(1, $t('login.password_missing')),
    username: z.string().min(1, $t('login.password_missing')),
    name: z.string().min(1, $t('login.password_missing')),

  });
  function updateAccount(email: string, username:string, password: string, name:string) {
    console.log(email);
    console.log(username);
    console.log(name);
    return 1;
  }
    let { form, errors, message, enhance, submitting } = lexSuperForm(
    formSchema,
    async () => {
      if (await updateAccount($form.email, $form.username, $form.password, $form.name)) {
        alert("yay, account not actually updated");
      }

    },
    {
      taintedMessage: false,
      clearOnSubmit: 'errors',
    }
  );    export let data: PageData;
let example_name = "John Doe";
let example_email = "johndoe@example.com";
let example_username = "jd";

  function updateName(newName: string){
    name=newName;
  }
</script>
<svelte:head>
  <title>Account Settings</title>
</svelte:head>
<!-- svelte-ignore a11y-missing-attribute -->
<div class = "shadow">
    <h1 class="card-title">Change account information</h1>
    <Page>

        <Form {enhance}>
          <Input
            id="email"
            label={$t('account_settings.label_email')}
            type="text"
            bind:value={$form.email}
            error={$errors.email}
            autofocus
            placeholder={example_email}
          />
          <Input
            id="name"
            label={$t('account_settings.label_name')}
            type="password"
            bind:value={$form.name}
            error={$errors.name}
            placeholder = {example_name}
            />
            <Input
            id="username"
            label={$t('account_settings.label_username')}
            type="text"
            bind:value={$form.username}
            error={$errors.username}
            autofocus
            placeholder = {example_username}
          />

          <a class="link mt-0" href="/forgotPassword">
            {$t('account_settings.forgot_password')}
          </a>


        </Form>
      </Page>

    </div>
