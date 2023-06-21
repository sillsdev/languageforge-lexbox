<script lang="ts">
    // This script handles the account settings page.
    // For now, it only allows you to modify the users display name
    // Minimal changes allow it to modify the users email as well (but this should have a re-verification system)
    import type { PageData } from './$types';
    import t from '$lib/i18n';
    import { Button, Form, Input, lexSuperForm } from '$lib/forms';
    import { Page } from '$lib/layout';
    import {_changeUserAccountData} from './+page';
    import type {ChangeUserAccountDataInput} from '$lib/gql/types';
    import {page} from '$app/stores';
    import { invalidate } from '$app/navigation';
    import z from 'zod';


    $: user = $page.data.user;
    page.subscribe(console.log);
    $: userid = user?.id;
    let newName: string;
    let newEmail: string;
    let success = true;


    const formSchema = z.object({
        email: z.string().email(),
        name: z.string(),
    });

    let { form, errors, enhance, submitting } = lexSuperForm(formSchema, async () => {await updateAccount();});


    async function updateAccount(): Promise<void> {
            const changeUserAccountDataInput: ChangeUserAccountDataInput = {
                email: $form.email,
                name: $form.name,
                userId: userid ?? '',
            };
            await _changeUserAccountData(changeUserAccountDataInput);
            if (user){
                await invalidate(`user:${user.id}`).then(() =>{
                    success = true;
                });
            }
    }
   export const data: PageData;
</script>

<Page>
    <div class="content-center">
            <Form {enhance}>
                <Input
                id="name"
                label={$t('account_settings.name')}
                type="text"
                error={$errors.name}
                bind:value={newName}
                placeholder={$user?.name}
            />
            <Input
                id="email"
                label={$t('account_settings.email')}
                type="email"
                error={$errors.email}
                bind:value={newEmail}
                autofocus
                placeholder={$user?.email}
                readonly={true}
            />
            <a class="link my-4" href="/resetPassword">
            {$t('account_settings.forgot_password')}
            </a>

            <Button loading={$submitting}>{$t('account_settings.button_update')}</Button>
            {#if success}
            <div class="alert alert-success mt-4">
                <span>{$t('account_settings.success')}</span>
              </div>
            {/if}
        </Form>
    </div>
</Page>
