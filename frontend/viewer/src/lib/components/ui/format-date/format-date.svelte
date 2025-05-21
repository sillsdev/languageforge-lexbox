<script lang="ts">
  import { DateFormatter, type DateValue, type ZonedDateTime } from '@internationalized/date';

  let { date, ...restProps } = $props<{ date: Date | ZonedDateTime }>();

  const formatter = new DateFormatter('en-US', {
    dateStyle: 'full',
    timeStyle: 'short',
  });

  const formattedDate = $derived(formatter.format(date));
</script>

<span {...restProps}>{formattedDate}</span>
