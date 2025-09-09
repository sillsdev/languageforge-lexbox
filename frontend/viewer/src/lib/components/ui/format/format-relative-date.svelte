<script lang="ts">
  import {formatRelativeDate} from './format-relative-date';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLTimeElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DurationFormatOptions;
  };

  const {
    date,
    defaultValue,
    options,
    ...restProps
  }: Props = $props();

  const formattedRelativeDate = $derived.by(() => {
    return formatRelativeDate(date, options, defaultValue);
  });
</script>

<time {...restProps}>{formattedRelativeDate}</time>
