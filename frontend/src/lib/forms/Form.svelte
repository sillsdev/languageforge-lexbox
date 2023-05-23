<script lang="ts">
	import type { EnhancedForm } from "sveltekit-superforms/client";

	export let enhance: EnhancedForm<any>["enhance"] | undefined = undefined;
	function enhance_if_requested(node: HTMLFormElement) {
		enhance && enhance(node);
	}
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form use:enhance_if_requested method="post" novalidate on:submit|preventDefault class="form-control">
	<slot />
</form>

<!-- see frontend/src/app.postcss for global styles related to forms -->
