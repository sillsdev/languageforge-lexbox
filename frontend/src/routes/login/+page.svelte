<script lang=ts>
	import { Button, ProtectedForm, Input, type Token } from '$lib/forms'
	import { Page } from '$lib/layout'
	import { login, logout } from '$lib/user'
	import { onMount } from 'svelte'

	onMount(logout)

	let email_or_username = ''
	let password = ''
	let missing_user_info = ''
	let missing_password = ''
	let short_password = ''
	let bad_credentials = false
	let robo_token = ''

	async function log_in() {
		missing_user_info = missing_password = short_password = ''

		if (! email_or_username) {
			missing_user_info = 'User info missing'
			return
		}
		if (! password) {
			missing_password = 'Password missing'
			return
		}
		// if (password.length < 7) {
		// 	short_password = 'Your password is pretty short, it should be at least 7 characters long'
		// 	return
		// }

		if (await login(email_or_username, password, robo_token)) {
			return window.location.pathname = '/' // force server hit for httpOnly cookies
		}

		bad_credentials = true
	}

	function store_token({ detail: { token }}: CustomEvent<Token>) {
		robo_token = token
	}
</script>

<Page>
	<svelte:fragment slot=header>Log in</svelte:fragment>

	<ProtectedForm on:submit={log_in} on:token={store_token}>
		<Input label='Email (or Send/Receive username)' type=email bind:value={email_or_username} error={missing_user_info} autofocus required />

		<Input label='Password' type=password bind:value={password} error={missing_password || short_password} required />

		{#if bad_credentials}
			<aside class='alert alert-error'>
				Something went wrong, please make sure you have used the correct account information.
			</aside>

			<Button>Try to log in again?</Button>
		{:else}
			<Button>Log in</Button>
		{/if}
	</ProtectedForm>
</Page>
