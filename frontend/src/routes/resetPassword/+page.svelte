<script>
    import Page from "$lib/layout/Page.svelte";
    import {Button, Form, Input, lexSuperForm, lexSuperValidate} from '$lib/forms';
    import t from '$lib/i18n';
    import {z} from 'zod';
    import {goto} from "$app/navigation";
    import {hash} from "$lib/user";

    const formSchema = z.object({
        password: z.string().min(4, $t('login.password_missing')),
    });
    let {form, errors, valid, update, reset, message, enhance, submitting} = lexSuperForm(formSchema);

    async function submit() {
        
        await lexSuperValidate($form, formSchema, update);
        if (!$valid) return;
        await fetch('api/login/resetPassword', {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({passwordHash: await hash($form.password)}),
        });
        await goto('/');
    }
</script>

<Page>
    <Form {enhance} on:submit={submit}>
        <Input bind:value={$form.password} type="password" label={$t('reset-password.new-password')}
               error={$errors.password} autofocus/>
        <Button loading={$submitting}>{ $t('reset-password.submit') }</Button>
    </Form>
</Page>