<script lang="ts" module>
  import {i18n} from '@lingui/core';
  import {locale} from 'svelte-i18n-lingui';
  import {fromStore} from 'svelte/store';

  const currentLocale = fromStore(locale);

  export function formatDate(
    value: Date | string | undefined | null,
    options?: Intl.DateTimeFormatOptions,
    defaultValue = '',
  ): string {
    if (!value) return defaultValue;
    void currentLocale.current; //invalidate when the current locale changes
    return i18n.date(value, {
      dateStyle: 'medium',
      timeStyle: 'short',
      ...options,
    });
  }
</script>

<script lang="ts">
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLSpanElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DateTimeFormatOptions;
  };

  const {date, defaultValue, options, ...restProps}: Props = $props();

  const formattedDate = $derived.by(() => {
    return formatDate(date, options, defaultValue);
  });

  const fullDate = $derived.by(() => {
    return formatDate(date, {dateStyle: 'full', timeStyle: 'long'});
  });
</script>

<span title={fullDate} {...restProps}>{formattedDate}</span>
