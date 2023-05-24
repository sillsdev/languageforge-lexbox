<script lang="ts">
	import type { SuperForm } from "sveltekit-superforms/client";

	let formElem: HTMLFormElement;
	export let id: string | undefined = undefined;
	export let enhance: SuperForm<any>["enhance"] | undefined = undefined;
	function enhance_if_requested(...args: Parameters<SuperForm<any>["enhance"]>) {
		enhance && enhance(...args);
	}

	export function requestSubmit() {
		formElem.requestSubmit();
	}
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form bind:this={formElem} {id} use:enhance_if_requested method="post" on:submit|preventDefault class="form-control">
	<slot />
</form>

<!-- see frontend/src/app.postcss for global styles related to forms -->
