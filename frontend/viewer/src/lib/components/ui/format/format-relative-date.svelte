<script lang="ts">
  import {formatRelativeDate} from './format-relative-date-fn.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {SvelteDate} from 'svelte/reactivity';

  type Props = HTMLAttributes<HTMLTimeElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DurationFormatOptions;
    live?: boolean | number;
  };

  const {
    date,
    defaultValue,
    options,
    live = false,
    ...restProps
  }: Props = $props();

  const now = new SvelteDate();
  let intervalId: number | undefined;

  $effect(() => {
    if (live) {
      intervalId = setInterval(() => {
        now.setTime(Date.now());
      }, typeof live === 'number' ? live : 1000);
    } else {
      if (intervalId !== undefined) {
        clearInterval(intervalId);
        intervalId = undefined;
      }
    }

    return () => {
      if (intervalId !== undefined) {
        clearInterval(intervalId);
      }
    };
  });

  const formattedRelativeDate = $derived.by(() => {
    return formatRelativeDate(date, options, {defaultValue: defaultValue || '', now});
  });
</script>

<time {...restProps}>{formattedRelativeDate}</time>
