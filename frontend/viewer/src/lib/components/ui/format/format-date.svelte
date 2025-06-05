<script lang="ts">
  import { DateFormatter } from '@internationalized/date';
  import {locale} from 'svelte-i18n-lingui';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLSpanElement> & {
    date: Date | undefined | null;
    defaultValue?: string;
  };

  const {
    date,
    defaultValue = '',
    ...restProps
  }: Props = $props();

  const formatter = $derived(new DateFormatter($locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }));

  const formattedDate = $derived(date ? formatter.format(date) : defaultValue);
</script>

<span {...restProps}>{formattedDate}</span>
