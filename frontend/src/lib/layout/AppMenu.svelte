<script>
	import { LogoutIcon } from '$lib/icons'
	import { user } from '$lib/user'
</script>

<div class=drawer-side on:click on:keydown>
	<div class=drawer-overlay></div>


	<!-- https://daisyui.com/components/menu  -->
	<ul class='menu bg-base-100 min-w-[33%]'>
		<header class='prose flex flex-col items-end p-4 mb-4'>
			<h2 class=mb-0>{$user.name}</h2>
			<span class=font-light>{$user.email}</span>
		</header>

		<li class=hover-bordered>
			<a href=/logout>Log out <LogoutIcon /></a>
		</li>

		<hr class=divider>
	</ul>
</div>

<style>
	a {
		justify-content: flex-end;
	}
</style>
