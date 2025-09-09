<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';
  import {formatRelativeDate, FormatRelativeDate} from '$lib/components/ui/format';
  import {SvelteDate} from 'svelte/reactivity';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    title: 'ui/format/FormatRelativeDate',
    argTypes: {
      live: {
        control: { type: 'boolean' },
      },
    },
    args: {
      live: false,
    },
  });

  let currentDate = new SvelteDate();

  // Create test dates
  const now = new Date();
  const oneMinuteAgo = new Date(now.getTime() - 60 * 1000);
  const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
  const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
  const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

  const oneMinuteLater = new Date(now.getTime() + 60 * 1000);
  const oneHourLater = new Date(now.getTime() + 60 * 60 * 1000);
  const oneDayLater = new Date(now.getTime() + 24 * 60 * 60 * 1000);
  const oneWeekLater = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);
</script>

<Story name="Basic Examples">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">1 minute ago (component):</div>
      <div><FormatRelativeDate date={oneMinuteAgo} live={args.live}/></div>

      <div class="font-medium">1 minute ago (function):</div>
      <div>{formatRelativeDate(oneMinuteAgo)}</div>

      <div class="font-medium">1 hour ago:</div>
      <div><FormatRelativeDate date={oneHourAgo} live={args.live}/></div>

      <div class="font-medium">1 day ago:</div>
      <div><FormatRelativeDate date={oneDayAgo} live={args.live}/></div>

      <div class="font-medium">1 week ago:</div>
      <div><FormatRelativeDate date={oneWeekAgo} live={args.live}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Future Dates">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">In 1 minute:</div>
      <div><FormatRelativeDate date={oneMinuteLater} live={args.live}/></div>

      <div class="font-medium">In 1 hour:</div>
      <div><FormatRelativeDate date={oneHourLater} live={args.live}/></div>

      <div class="font-medium">In 1 day:</div>
      <div><FormatRelativeDate date={oneDayLater} live={args.live}/></div>

      <div class="font-medium">In 1 week:</div>
      <div><FormatRelativeDate date={oneWeekLater} live={args.live}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Live Updates" args={{ live: true }}>
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="p-4 border rounded-lg">
      <h3 class="font-bold mb-4">Live Updates Demo (updates every second)</h3>
      <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
        <div class="font-medium">Current time:</div>
        <div><FormatRelativeDate date={currentDate} live={args.live}/></div>

        <div class="font-medium">30 seconds ago:</div>
        <div><FormatRelativeDate date={new Date(Date.now() - 30000)} live={args.live}/></div>

        <div class="font-medium">2 minutes ago:</div>
        <div><FormatRelativeDate date={new Date(Date.now() - 120000)} live={args.live}/></div>
      </div>
      <p class="text-sm text-muted-foreground mt-4">
        Toggle the "live" control to see the difference between static and live updates.
      </p>
    </div>
  {/snippet}
</Story>

<Story name="Edge Cases">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">Null date:</div>
      <div><FormatRelativeDate date={null} defaultValue="No date" live={args.live}/></div>

      <div class="font-medium">Undefined date:</div>
      <div><FormatRelativeDate date={undefined} defaultValue="Unknown" live={args.live}/></div>

      <div class="font-medium">String date:</div>
      <div><FormatRelativeDate date="2024-01-01T00:00:00Z" live={args.live}/></div>

      <div class="font-medium">Current time:</div>
      <div><FormatRelativeDate date={currentDate} live={args.live}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Formatting Options">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">Default style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} live={args.live}/></div>

      <div class="font-medium">Narrow style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} options={{style: 'narrow'}} live={args.live}/></div>

      <div class="font-medium">Short style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} options={{style: 'short'}} live={args.live}/></div>

      <div class="font-medium">Digital style (1 hour ago):</div>
      <div><FormatRelativeDate date={oneHourAgo} options={{style: 'digital'}} live={args.live}/></div>
    </div>
  {/snippet}
</Story>
