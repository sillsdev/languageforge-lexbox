<script lang="ts">
    import {Page} from '$lib/layout';
    import t from '$lib/i18n';
    import {Input, ProtectedForm, lexSuperForm, lexSuperValidate, Button} from '$lib/forms';
    import {z} from 'zod';
    import {register} from '$lib/user';
    import {goto} from '$app/navigation';

    const formSchema = z.object({
        name: z.string().min(1, $t('register.name_missing')),
        email: z.string().email($t('register.email')),
        password: z.string().min(1, $t('register.password_missing')),
    });
    let {form, errors, valid, update, reset, message} = lexSuperForm(formSchema);
    let turnstileToken = '';

    async function submit() {
        await lexSuperValidate($form, formSchema, update);
        if (!$valid) return;
        const {user, error} = await register($form.password, $form.name, $form.email, turnstileToken);
        if (error) {
            if (error.turnstile) {
                $message = $t('register.turnstile_error');
            }
            if (error.accountExists) {
                $message = $t('register.account_exists');
            }
            return;
        }
        if (user) {
            goto('/');
            return;
        }
        throw new Error('Unknown error, no error from server, but also no user.');
    }
</script>

<Page>
    <svelte:fragment slot="header">{$t('register.title')}</svelte:fragment>

    <ProtectedForm bind:turnstileToken={turnstileToken} on:submit={submit}>
        <Input
                id="name"
                label={$t('register.label_name')}
                required
                bind:value={$form.name}
                error={$errors.name}
        />
        <Input
                id="email"
                label={$t('register.label_email')}
                type="email"
                required
                bind:value={$form.email}
                error={$errors.email}
        />
        <Input
                id="password"
                label={$t('register.label_password')}
                type="password"
                required
                bind:value={$form.password}
                error={$errors.password}
        />
        {#if $message}
            <aside class="alert alert-error">
                {$message}
            </aside>
        {/if}
        <Button>{$t('register.button_register')}</Button>
    </ProtectedForm>
</Page>
