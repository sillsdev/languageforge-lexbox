<script lang="ts">
	import Badge from './Badge.svelte';

    // Add more as necessary. Should be as limited as possible to maximize consistency.
    type BadgeButtonType = 'badge-success';
	export let type: BadgeButtonType = 'badge-success';
	export let icon = '';
	export let disabled = false;

    $: _type = type as any;
</script>

<button on:click {disabled} class="button-badge">
	<Badge type={_type} {icon}>
		<slot />
	</Badge>
</button>
