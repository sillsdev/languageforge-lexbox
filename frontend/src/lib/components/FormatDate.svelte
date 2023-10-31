<script lang="ts" context="module">
  type FormatStyle = 'full' | 'long' | 'medium' | 'short' | 'none';
</script>

<script lang="ts">
  import { locale } from 'svelte-intl-precompile';
  export let date: string | Date | number | null | undefined;
  export let timeFormat: FormatStyle = 'short';
  export let dateFormat: FormatStyle = 'short';
  export let nullLabel = '–';

  $: formatter = new Intl.DateTimeFormat($locale, {
    dateStyle: dateFormat === 'none' ? undefined : dateFormat,
    timeStyle: timeFormat === 'none' ? undefined : timeFormat,
  });
  $: dateString = date ? formatter.format(new Date(date)) : null;
</script>

<span>{dateString ?? nullLabel}</span>
