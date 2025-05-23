<script lang="ts">
  import { DateFormatter, type ZonedDateTime } from '@internationalized/date';
  import {locale} from 'svelte-i18n-lingui';

  let { date, ...restProps } = $props<{ date: Date | ZonedDateTime }>();

  const formatter = new DateFormatter($locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  });

  const formattedDate = $derived(formatter.format(date));
</script>

<span {...restProps}>{formattedDate}</span>
