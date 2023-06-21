<script lang="ts">
    // This script handles the account settings page.
    // For now, it only allows you to modify the users display name
    // Minimal changes allow it to modify the users email as well (but this should have a re-verification system)
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import { Button, Form, Input, lexSuperForm } from '$lib/forms';
    import { Page } from '$lib/layout';
    import {_changeUserAccountData} from "./+page";
    import type {ChangeUserAccountDataInput} from '$lib/gql/types';
    import {page} from "$app/stores";
    import { invalidate } from '$app/navigation';
    // Get users data (reactive)
    $: user = $page.data.user;
    page.subscribe(console.log);
    $: users_name = user?.name; // not to be confused with username
    $: email = user?.email;
    $: userid = user?.id;
    let newName: string;
    let changed = false;
    //user.set({ ...get(user), name: result.name, })
    // This function updates the account information on the server
    async function updateAccount(
      email: string | undefined,
      name: string | null,
    ) {
      if (confirm($t('account_settings.confirm_change'))) {
        // prepare the input
        const changeUserAccountDataInput: ChangeUserAccountDataInput = {
            email: email ?? '',
            name: name ?? '',
            userId: userid ?? '',
        };
        await _changeUserAccountData(changeUserAccountDataInput);
        if (user){
            invalidate(`user:${user.id}`);
        }
        changed = true;
      }
    }
   export let data: PageData;
</script>

<Page>
    <div class="content-center">
        <Form>
            <Input
                id="email"
                label={$t('account_settings.email')}
                type="email"
                bind:value={email}
                autofocus
                placeholder={email}
                readonly={true}
            />
            <Input
                id="name"
                label={$t('account_settings.name')}
                type="text"
                bind:value={newName}
                placeholder={users_name}
            />

            <a class="link my-4" href="/forgotPassword">
            {$t('account_settings.forgot_password')}
            </a>

            <Button on:click={()=>{updateAccount(email, newName);}}>{$t('account_settings.button_update')}</Button>
            {#if changed}
                <p class="text-warning">{$t("account_settings.notify")}</p>
            {/if}
        </Form>
    </div>
</Page>
