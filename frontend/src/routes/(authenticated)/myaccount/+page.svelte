<script lang="ts">
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import { Button, Form, Input, lexSuperForm } from '$lib/forms';
    import { Page } from '$lib/layout';
    import { z } from 'zod';
    import { user } from '$lib/user';

    const formSchema = z.object({ //not entirely sure what this is
        email: z.string().min(1, $t('account_settings.missing_user_info')),
        password: z.string().min(1, $t('login.password_missing')),
        username: z.string().min(1, $t('login.password_missing')),
        name: z.string().min(1, $t('login.password_missing')),
    });

    let example_name = "John Doe";
    let example_email = "johndoe@example.com";
    let example_username = "jd";
    $: userid = $user?.id;


    function updateAccount(email: string, username:string, password: string, name:string) {
    if(confirm($t("account_settings.confirm_change"))){
        alert(userid);
        console.log(email);
        console.log(username);
        console.log(name);
        return true;
    }else{
        return false;
        }
    }
    let { form, errors, message, enhance, submitting } = lexSuperForm(
    formSchema,
    () => {
        alert('hi');
      if (updateAccount($form.email, $form.username, $form.password, $form.name)) {
        alert("yay, account not actually updated");
      }

    },
    {
      taintedMessage: false,
      clearOnSubmit: 'errors',
    }
  );    export let data: PageData;

</script>
<svelte:head>
  <title>Account Settings</title>
</svelte:head>
<!-- svelte-ignore a11y-missing-attribute -->
<div class = "content-center ">
    <h1 class="card-title justify-center text-3xl">Change account information</h1>
    <br><br><br>
    <Page>

        <Form {enhance}>
          <Input
            id="email"
            label={$t('account_settings.label_email')}
            type="text"
            bind:value={$form.email}
            autofocus
            placeholder={example_email}
          />
          <Input
            id="name"
            label={$t('account_settings.label_name')}
            type="password"
            bind:value={$form.name}
            placeholder = {example_name}
            />


          <a class="link mt-0" href="/forgotPassword">
            {$t('account_settings.forgot_password')}
          </a>
          <Button on:click={()=>{updateAccount($form.email, $form.username, $form.password, $form.name)}}>{$t('account_settings.button_update')}</Button>
        </Form>
        <br>

      </Page>

    </div>
