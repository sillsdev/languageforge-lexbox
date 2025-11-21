<script lang="ts">
  import {formatRelativeDate} from './format-relative-date-fn.svelte';
  import {formatDate} from './format-date.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {SvelteDate} from 'svelte/reactivity';
  import Icon from '../icon/icon.svelte';
  import * as Popover from '../popover';
  import type {SmallestUnit} from './format-duration';

  type Props = HTMLAttributes<HTMLTimeElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DurationFormatOptions;
    live?: boolean | number;
    showActualDate?: boolean;
    actualDateOptions?: Intl.DateTimeFormatOptions;
    maxUnits?: number;
    smallestUnit?: SmallestUnit,
    loading?: boolean;
  };

  const {
    date,
    defaultValue,
    options,
    live = false,
    showActualDate = false,
    actualDateOptions,
    maxUnits = 2,
    smallestUnit = 'seconds',
    loading,
    ...restProps
  }: Props = $props();

  const now = new SvelteDate();
  let intervalId: ReturnType<typeof setInterval> | undefined;

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
    return formatRelativeDate(date, options, {defaultValue: defaultValue || '', now, maxUnits, smallestUnit});
  });

  const actualFormattedDate = $derived.by(() => {
    if (!showActualDate || !date) return '';
    return formatDate(date, actualDateOptions);
  });

  const fullDate = $derived.by(() => {
    if (!date) return '';
    return formatDate(date, { dateStyle: 'full', timeStyle: 'long' });
  });
</script>

{#if showActualDate && actualFormattedDate}
  <span class:loading-text={loading} class="inline-flex items-center gap-1">
    <Popover.Root>
      <Popover.InfoTrigger>
        <time title={fullDate} {...restProps}>{formattedRelativeDate}</time>
      </Popover.InfoTrigger>
      <Popover.Content class="w-auto p-2 text-sm">
        <Icon icon="i-mdi-calendar-clock" class="text-muted-foreground mr-1" />
        {actualFormattedDate}
      </Popover.Content>
    </Popover.Root>
  </span>
{:else}
  <time title={fullDate} class:loading-text={loading} {...restProps}>{formattedRelativeDate}</time>
{/if}
