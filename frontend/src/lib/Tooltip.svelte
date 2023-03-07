<script context="module">
	// // https://www.skeleton.dev/utilities/popups
	import { computePosition, autoUpdate, flip, shift, offset, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';

	storePopup.set({ computePosition, autoUpdate, flip, shift, offset, arrow });
</script>

<script lang="ts">
	import { popup, type PopupSettings } from '@skeletonlabs/skeleton';

	export let text: string;

	const settings: PopupSettings = {
		event: 'hover',
		target: `popup-${ Date.now() }`,
		placement: 'left',
	};
</script>

<span use:popup={settings} class="pl-4">
	<slot />
</span>

<div class="card variant-filled-tertiary p-4" data-popup={settings.target}>
	{text}
	<div class="arrow variant-filled-tertiary" />
</div>
