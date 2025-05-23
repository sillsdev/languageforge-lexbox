<script lang="ts">
  import { DateFormatter } from '@internationalized/date';
  import {locale} from 'svelte-i18n-lingui';

  let { date, ...restProps }: { date: Date | undefined } = $props();

  const formatter = new DateFormatter($locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  });

  const formattedDate = $derived(date ? formatter.format(date) : '');
</script>

<span {...restProps}>{formattedDate}</span>
