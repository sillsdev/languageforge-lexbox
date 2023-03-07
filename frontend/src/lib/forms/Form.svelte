<script>
	import { enhance } from '$app/forms';
</script>

<form method=POST novalidate use:enhance>
	<slot />
</form>

<!-- see frontend/src/app.postcss for global styles related to forms -->
