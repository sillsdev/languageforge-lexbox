<script>
	import { user } from '$lib/user'
	import AdminDashboard from './(dashboards)/AdminDashboard.svelte'
	import UserDashboard from './(dashboards)/UserDashboard.svelte'
</script>

{#if $user?.role === 'admin'}
	<AdminDashboard />
{:else}
	<UserDashboard />
{/if}
