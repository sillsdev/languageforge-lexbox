<script lang="ts">
	import Page from '$lib/layout/Page.svelte';
	import { z } from 'zod';
	import t from '$lib/i18n';
	import { lexSuperForm } from '$lib/forms';
	import { Form, Input } from '$lib/forms';
	import Button from '$lib/forms/Button.svelte';
	import { goto } from '$app/navigation';

	const formSchema = z.object({
		email: z.string().email($t('register.email')),
	});
	let { form, errors, enhance, submitting } = lexSuperForm(formSchema, async ({ form }) => {
		await fetch(`api/login/forgotPassword?email=${$form.email}`, { method: 'POST' });
		await goto('/forgotPassword/emailSent');
	});
</script>

<Page>
	<Form {enhance}>
		<Input
			id="email"
			label={$t('register.label_email')}
			autofocus
			type="email"
			required
			bind:value={$form.email}
			error={$errors.email}
		/>
		<Button loading={$submitting}>{$t('forgot_password.send_email')}</Button>
	</Form>
</Page>
