<script lang="ts">
    // This script handles the account settings page
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import { Button, Form, Input, lexSuperForm } from '$lib/forms';
    import { Page } from '$lib/layout';
    import { z } from 'zod';
    import { user } from '$lib/user';
    import {_changeUserAccountData} from "./+page";
    import type {ChangeUserAccountDataInput} from '$lib/gql/types';
    import Email from '$lib/email/Email.svelte';
    // This schema defines the shape and validation rules for the form data
    const formSchema = z.object({
      email: z.string().email($t('account_settings.invalid_email')),
      password: z.string().min(8, $t('account_settings.password_too_short')),
      username: z.string().nonempty($t('account_settings.username_required')),
      name: z.string().nonempty($t('account_settings.name_required')),
    });

    // These are reactive variables that get the current user's data
    $: example_name = $user?.name;
    $: example_email = $user?.email;
    $: userid = $user?.id;

    // This function updates the account information on the server
    async function updateAccount(
      email: string,
      username: string,
      userid: string,
      name: string
    ) {
      if (confirm($t('account_settings.confirm_change'))) {
        // TODO: make an API call to update the user data
        const changeUserAccountDataInput: ChangeUserAccountDataInput = {
            email: email,
            name: name,
            username: username,
            userId: userid,
        };

        console.log(userid);
        console.log(email);
        console.log(username);
        console.log(name);
        _changeUserAccountData(changeUserAccountDataInput);
        return true;
      } else {
        return false;
      }
    }


    let { form, errors, message, enhance, submitting } = lexSuperForm(
      formSchema,
      async () => {
        // This is the submit handler for the form
        if (await updateAccount($form.email, $form.username, $form.password, $form.name)) {
          alert('yay, account updated');
        }
      },
      {
        taintedMessage: 'not actually sure what this is',
        clearOnSubmit: 'errors',
      }
    );
    export let data: PageData;
  </script>
  <svelte:head>
    <title>Account Settings</title>
  </svelte:head>
  <div class="content-center">
    <h1 class="card-title justify-center text-3xl">Change account information</h1>
    <br /><br /><br />
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
          type="text"
          bind:value={$form.name}
          placeholder={example_name}
        />


        <a class="link mt-0" href="/forgotPassword">
          {$t('account_settings.forgot_password')}
        </a>
        <Button on:click={()=>{updateAccount($form.email, $form.username, $form.name);}}>{$t('account_settings.button_update')}</Button>
      </Form>
      <br />
    </Page>
  </div>

