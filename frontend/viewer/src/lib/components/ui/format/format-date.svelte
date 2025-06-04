<script lang="ts" module>
  export function formatDate(value: Date | string | undefined | null, options?: Intl.DateTimeFormatOptions, defaultValue = ''): string {
    if (!value) return defaultValue;
    return i18n.date(value, {
      dateStyle: 'medium',
      timeStyle: 'short',
      ...options,
    });
  }
</script>

<script lang="ts">
  import type {HTMLAttributes} from 'svelte/elements';
  import {i18n} from '@lingui/core';

  type Props = HTMLAttributes<HTMLSpanElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DateTimeFormatOptions;
  };

  const {
    date,
    defaultValue,
    options,
    ...restProps
  }: Props = $props();

  const formattedDate = $derived(formatDate(date, options, defaultValue));
</script>

<span {...restProps}>{formattedDate}</span>
