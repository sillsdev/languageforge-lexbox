<script lang="ts">
	export let type = '';
	export let icon = '';
</script>

<span class="badge badge-lg {type} p-4 whitespace-nowrap">
    <slot />

    {#if icon}
        <span class="{icon} text-lg ml-2" />
    {/if}
</span>
