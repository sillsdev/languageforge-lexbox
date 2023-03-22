<script lang="ts" context="module">
	type formatStyle = "full" | "long" | "medium" | "short" | "none"
</script>

<script lang="ts">
	import { locale } from 'svelte-intl-precompile';
    export let date: string | Date | null;
    export let timeFormat: formatStyle = 'short';
    export let dateFormat: formatStyle = 'short';
    export let nullLabel = 'null';

	$: formatter = new Intl.DateTimeFormat($locale, {
        dateStyle: dateFormat === "none" ? undefined : dateFormat,
        timeStyle: timeFormat === "none" ? undefined : timeFormat,
    });
    $: dateString = date ? formatter.format(new Date(date)) : null;
</script>

<span>{dateString ?? nullLabel}</span>
