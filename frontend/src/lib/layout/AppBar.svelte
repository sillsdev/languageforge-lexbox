<script lang=ts>
	import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons'
	import { user } from '$lib/user'
	import { createEventDispatcher } from 'svelte'

	const dispatch = createEventDispatcher()
</script>

<!-- https://daisyui.com/components/navbar -->
<header class='navbar bg-primary text-primary-content'>
	<span class='navbar-start ml-4 text-lg md:text-3xl tracking-wider'>
		Language Depot
	</span>

	<div class=navbar-end>
		{#if $user}
			<button on:click={ () => dispatch('menuopen') } class='btn btn-primary btn-circle'>
				<AuthenticatedUserIcon />
			</button>
		{:else}
			<div class='tooltip tooltip-left' data-tip='Create a new account'>
				<a href=/account/create class='btn btn-primary btn-circle'>
					<UserAddOutline />
				</a>
			</div>
		{/if}
	</div>
</header>
