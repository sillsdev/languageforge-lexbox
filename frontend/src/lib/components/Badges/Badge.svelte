<script lang="ts">

    // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
    type BadgeType = undefined |  'badge-info';
	export let type: BadgeType = undefined;
	export let icon = '';
</script>

<span class="badge badge-lg {type} p-4 whitespace-nowrap">
    <slot />

    {#if icon}
        <span class="{icon} text-lg ml-2" />
    {/if}
</span>
