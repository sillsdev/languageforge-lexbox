<script lang="ts">
  import {formatRelativeDate} from './format-relative-date-fn.svelte';
  import {formatDate} from './format-date.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {SvelteDate} from 'svelte/reactivity';
  import Icon from '../icon/icon.svelte';
  import * as Popover from '../popover/index.js';

  type Props = HTMLAttributes<HTMLTimeElement> & {
    date: Date | string | undefined | null;
    defaultValue?: string;
    options?: Intl.DurationFormatOptions;
    live?: boolean | number;
    showActualDate?: boolean;
    actualDateOptions?: Intl.DateTimeFormatOptions;
    maxUnits?: number;
  };

  const {
    date,
    defaultValue,
    options,
    live = false,
    showActualDate = false,
    actualDateOptions,
    maxUnits,
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
    return formatRelativeDate(date, options, {defaultValue: defaultValue || '', now, maxUnits});
  });

  const actualFormattedDate = $derived.by(() => {
    if (!showActualDate || !date) return '';
    return formatDate(date, actualDateOptions);
  });
</script>

{#if showActualDate && actualFormattedDate}
  <span class="inline-flex items-center gap-1">
    <time {...restProps}>{formattedRelativeDate}</time>
    <Popover.Root>
      <Popover.Trigger>
        <Icon icon="i-mdi-information-outline" class="size-4 text-muted-foreground hover:text-foreground" />
      </Popover.Trigger>
      <Popover.Content class="w-auto">
        <Icon icon="i-mdi-calendar-clock" class="text-muted-foreground mr-1" />
        {actualFormattedDate}
      </Popover.Content>
    </Popover.Root>
  </span>
{:else}
  <time {...restProps}>{formattedRelativeDate}</time>
{/if}
