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
      name: string
    ) {
      if (confirm($t('account_settings.confirm_change'))) {
        // TODO: make an API call to update the user data
        alert(userid);
        const changeUserAccountDataInput: ChangeUserAccountDataInput = {
            email: email,
            name: name,
            userId: userid || '',
        };

        _changeUserAccountData(changeUserAccountDataInput);
        return true;
      } else {
        return false;
      }
    }


    let { form, errors, message, enhance, submitting } = lexSuperForm(
      formSchema,
      async () => {

        if (await updateAccount($form.email, $form.name)) {
          alert('yay, account updated');
        }
      },
      {
        taintedMessage: false,
        clearOnSubmit: 'errors',
      }
    );
    export let data: PageData;
  </script>


  <svelte:head>
    <title>Account Settings</title>
  </svelte:head>
  <div class="content-center">
    <h1 class="card-title justify-center text-3xl mb-32">Change account information</h1>
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
        <!-- <Input
        id="username"
        label={$t('account_settings.label_name')}
        type="text"
        bind:value={$form.username}
        placeholder={example_username}
      /> -->

        <a class="link my-4" href="/forgotPassword">
          {$t('account_settings.forgot_password')}
        </a>
        <Button on:click={()=>{updateAccount($form.email, $form.name);}}>{$t('account_settings.button_update')}</Button>
      </Form>
    </Page>
  </div>

