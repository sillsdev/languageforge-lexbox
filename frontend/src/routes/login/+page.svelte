<script lang="ts">
	import { Button, Form, Input } from '$lib/forms';
	import { Page } from '$lib/layout';
	import type { ActionData } from './$types';

	export let form: ActionData;
</script>

<Page>
	<svelte:fragment slot="header">Log in</svelte:fragment>

	<Form>
		<Input label="Email (or Send/Receive username)" type="email" name="user_id" value={form?.user_id} error={form?.error?.user_id} autofocus required />

		<Input label="Password" type="password" name="password" error={form?.error?.password} required />

		{#if form?.error?.bad_credentials}
			<aside class="alert variant-ghost-error">
				<div class="alert-message">
					<p>
						Something went wrong, please make sure you have used the correct account information.
					</p>
				</div>
			</aside>

			<Button wide>Try to log in again?</Button>
		{:else}
			<Button wide>Log in</Button>
		{/if}
	</Form>
</Page>
