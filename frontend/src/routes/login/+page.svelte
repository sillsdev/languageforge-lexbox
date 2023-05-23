<script lang="ts">
	import { Button, Form, Input, lexSuperForm, lexSuperValidate } from '$lib/forms';
	import t from '$lib/i18n';
	import { Page } from '$lib/layout';
	import {login, logout, user} from '$lib/user';
	import { onMount } from 'svelte';
	import { z } from 'zod';
	import {goto} from "$app/navigation";

	const formSchema = z.object({
		email: z.string().min(1, $t('login.missing_user_info')),
		password: z.string().min(1, $t('login.password_missing')),
	});
	let { form, errors, valid, update, reset, message, enhance } = lexSuperForm(formSchema, {taintedMessage: false});

	onMount(logout);
	let bad_credentials = false;

	async function log_in() {
		await lexSuperValidate($form, formSchema, update);
		if (!$valid) return;

		if (await login($form.email, $form.password)) {
			await goto($user?.role == 'admin' ? '/admin' : '/')
			return;
		}

		$message = $t('login.bad_credentials');
		bad_credentials = true;
	}
</script>

<Page>
	<svelte:fragment slot="header">{$t('login.page_header')}</svelte:fragment>

	<Form {enhance} on:submit={log_in}>
		<Input
			id="email"
			label={$t('login.label_email')}
			type="email"
			bind:value={$form.email}
			error={$errors.email}
			autofocus
			required
		/>

		<Input
			id="password"
			label={$t('login.label_password')}
			type="password"
			bind:value={$form.password}
			error={$errors.password}
			required
		/>
		<a class="link mt-0" href="/forgotPassword">
			{ $t('login.forgot-password') }
		</a>

		{#if bad_credentials}
			<aside class="alert alert-error">
				{$message}
			</aside>

			<Button>{$t('login.button_login_again')}</Button>
		{:else}
			<Button>{$t('login.button_login')}</Button>
		{/if}
		<a class="btn btn-primary" href="/register">Register</a>
	</Form>
</Page>
