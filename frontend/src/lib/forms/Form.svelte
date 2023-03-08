<script>
	import { enhance } from '$app/forms'
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form method=post novalidate use:enhance class=form-control>
	<slot />
</form>

<!-- see frontend/src/app.postcss for global styles related to forms -->
