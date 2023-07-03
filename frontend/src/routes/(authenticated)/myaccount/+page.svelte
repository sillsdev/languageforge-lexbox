<script lang="ts">
    // This script handles the account settings page.
    // For now, it only allows you to modify the users display name
    // Minimal changes allow it to modify the users email as well (but this should have a re-verification system)
    import t from '$lib/i18n';
    import { Button, Form, Input } from '$lib/forms';
    import { Page } from '$lib/layout';
    import {_changeUserAccountData} from './+page';
    import type {ChangeUserAccountDataInput} from '$lib/gql/types';
    import type { PageData } from './$types';

    export let data: PageData;

    // Get users data (reactive)
    $: user = data.user;
    $: users_name = user?.name; // not to be confused with username
    $: email = user?.email;
    $: userid = user?.id;
    let newName: string = users_name || '';
    let changed = false;

    // This function updates the account information on the server
    async function updateAccount(
      email: string | undefined,
      name: string | null,
    ): Promise<void> {
      if (confirm($t('account_settings.confirm_change'))) {
        // prepare the input
        const changeUserAccountDataInput: ChangeUserAccountDataInput = {
            email: email || '',
            name: name || '',
            userId: userid || '',
        };
        await _changeUserAccountData(changeUserAccountDataInput);
        changed = true;
      }
    }
</script>

<Page>
    <svelte:fragment slot="header">
        {$t('account_settings.title')}
    </svelte:fragment>
    <div class="content-center">
        <h1 class="card-title justify-center text-3xl mt-10 mb-28">{$t('account_settings.page_h1')}</h1>
        <Form>
            <Input
                id="email"
                label={$t('account_settings.label_email')}
                type="email"
                value={email}
                autofocus
                placeholder={email}
                readonly={true}
            />
            <Input
                id="name"
                label={$t('account_settings.label_name')}
                type="text"
                bind:value={newName}
                placeholder={users_name}
            />

            <a class="link my-4" href="/forgotPassword">
            {$t('account_settings.forgot_password')}
            </a>

            <Button on:click={() => updateAccount(email, newName)}>{$t('account_settings.button_update')}</Button>
            {#if changed}
                <p class="text-warning">{$t('account_settings.notify')}</p>
            {/if}
        </Form>
    </div>
</Page>
