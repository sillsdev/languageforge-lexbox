<script lang="ts">
	import { env } from '$env/dynamic/public';
	import t from '$lib/i18n';
	import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
	import { user } from '$lib/user';
	import { createEventDispatcher } from 'svelte';

	let environmentName = env.PUBLIC_ENV_NAME;
	const dispatch = createEventDispatcher();
</script>

<!-- https://daisyui.com/components/navbar -->
<header class="navbar bg-primary text-primary-content">
	<span class="navbar-start ml-4 text-lg md:text-3xl tracking-wider">
		{$t('appbar.app_name')}
	</span>
	{#if environmentName !== 'production'}
		<div class="alert alert-warning justify-center">
			<span>
				{ $t('environment-warning', {environmentName}) }
			</span>
		</div>
	{/if}
	<div class="navbar-end">
		{#if $user}
			<button on:click={() => dispatch('menuopen')} class="btn btn-primary btn-circle">
				<AuthenticatedUserIcon />
			</button>
		{:else}
			<div class="tooltip tooltip-left" data-tip="Create a new account">
				<a href="/register" class="btn btn-primary btn-circle">
					<UserAddOutline />
				</a>
			</div>
		{/if}
	</div>
</header>
