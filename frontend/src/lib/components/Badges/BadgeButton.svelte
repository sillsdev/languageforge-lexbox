<script lang="ts">
	import { Badge } from ".";

	export let type = '';
	export let icon = '';
	export let disabled = false;
</script>

<button on:click {disabled} class="button-badge">
    <Badge {type} {icon}>
        <slot />
    </Badge>
</button>
