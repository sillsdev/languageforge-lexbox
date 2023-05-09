<script lang="ts">
    import Page from "$lib/layout/Page.svelte";
    import {z} from "zod";
    import t from '$lib/i18n';
    import {lexSuperForm, lexSuperValidate} from "$lib/forms";
    import {Form, Input} from "$lib/forms";
    import Button from "$lib/forms/Button.svelte";
    import {goto} from "$app/navigation";

    const formSchema = z.object({
        email: z.string().email($t('register.email')),
    });
    let {form, errors, valid, update, enhance} = lexSuperForm(formSchema);

    async function submit() {
        await lexSuperValidate($form, formSchema, update);
        if (!$valid) return;
        await fetch(`api/login/forgotPassword?email=${$form.email}`, {method: 'POST'});
        goto('/forgotPassword/emailSent');
    }
</script>

<Page>
    <Form {enhance} on:submit={submit}>
        <Input
            id="email"
            label={$t('register.label_email')}
            autofocus
            type="email"
            required
            bind:value={$form.email}
            error={$errors.email}
    />
        <Button>{$t('forgot_password.send_email')}</Button>
    </Form>
</Page>