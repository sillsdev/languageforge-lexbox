<script>
	import { AppBar } from '@skeletonlabs/skeleton';
	import Icon from '$lib/Icon.svelte';
	import { user } from '$lib/user';
	import Tooltip from '../Tooltip.svelte';
	import { open_menu } from '.';
	import { ButtonIcon } from '$lib/forms';
</script>

<AppBar background="bg-primary-500 text-on-primary-token">
	<svelte:fragment slot="lead">
		<strong class="text-lg">Language Depot</strong>
	</svelte:fragment>

	<svelte:fragment slot="trail">
		{#if $user}
			<ButtonIcon on:click={open_menu} name="healthicons:ui-user-profile" />
		{:else}
			<Tooltip text="Create a new account">
				<a href="/account/create">
					<Icon name="mdi:user-add-outline" />
				</a>
			</Tooltip>
		{/if}
	</svelte:fragment>
</AppBar>
