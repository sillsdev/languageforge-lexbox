<script module lang="ts">
  import { defineMeta } from '@storybook/addon-svelte-csf';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';
  import {formatRelativeDate, FormatRelativeDate} from '$lib/components/ui/format';
  import {SvelteDate} from 'svelte/reactivity';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    argTypes: {
      live: {
        control: { type: 'boolean' },
      },
      showActualDate: {
        control: { type: 'boolean' },
      },
      maxUnits: {
        control: { type: 'number', min: 1, max: 5, step: 1 },
      },
    },
    args: {
      live: false,
      showActualDate: false,
      maxUnits: undefined,
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

  // Complex dates for maxUnits demo
  const complexPast = new Date(now.getTime() - (7 * 24 * 60 * 60 * 1000 + 10 * 60 * 60 * 1000 + 17 * 60 * 1000 + 30 * 1000 + 500)); // 7 days, 10 hours, 17 minutes, 30 seconds, 500ms ago
  const moderatePast = new Date(now.getTime() - (2 * 60 * 60 * 1000 + 45 * 60 * 1000 + 30 * 1000)); // 2 hours, 45 minutes, 30 seconds ago
</script>

<Story name="Basic Examples">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">1 minute ago (component):</div>
      <div><FormatRelativeDate date={oneMinuteAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">1 minute ago (function):</div>
      <div>{formatRelativeDate(oneMinuteAgo)}</div>

      <div class="font-medium">1 hour ago:</div>
      <div><FormatRelativeDate date={oneHourAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">1 day ago:</div>
      <div><FormatRelativeDate date={oneDayAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">1 week ago:</div>
      <div><FormatRelativeDate date={oneWeekAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Future Dates">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">In 1 minute:</div>
      <div><FormatRelativeDate date={oneMinuteLater} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">In 1 hour:</div>
      <div><FormatRelativeDate date={oneHourLater} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">In 1 day:</div>
      <div><FormatRelativeDate date={oneDayLater} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">In 1 week:</div>
      <div><FormatRelativeDate date={oneWeekLater} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
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
        <div><FormatRelativeDate date={currentDate} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

        <div class="font-medium">30 seconds ago:</div>
        <div><FormatRelativeDate date={new Date(Date.now() - 30000)} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

        <div class="font-medium">2 minutes ago:</div>
        <div><FormatRelativeDate date={new Date(Date.now() - 120000)} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
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
      <div><FormatRelativeDate date={null} defaultValue="No date" live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">Undefined date:</div>
      <div><FormatRelativeDate date={undefined} defaultValue="Unknown" live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">String date:</div>
      <div><FormatRelativeDate date="2024-01-01T00:00:00Z" live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">Current time:</div>
      <div><FormatRelativeDate date={currentDate} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Formatting Options">
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
      <div class="font-medium">Default style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">Narrow style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} options={{style: 'narrow'}} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">Short style (1 day ago):</div>
      <div><FormatRelativeDate date={oneDayAgo} options={{style: 'short'}} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

      <div class="font-medium">Digital style (1 hour ago):</div>
      <div><FormatRelativeDate date={oneHourAgo} options={{style: 'digital'}} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
    </div>
  {/snippet}
</Story>

<Story name="Info Icon Demo" args={{ showActualDate: true }}>
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="p-4 border rounded-lg">
      <h3 class="font-bold mb-4">Info Icon with Popover Demo</h3>
      <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
        <div class="font-medium">1 hour ago (hover for actual date):</div>
        <div><FormatRelativeDate date={oneHourAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

        <div class="font-medium">2 days ago:</div>
        <div><FormatRelativeDate date={oneDayAgo} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>

        <div class="font-medium">Custom date format:</div>
        <div><FormatRelativeDate date={oneWeekAgo} live={args.live} showActualDate={args.showActualDate} actualDateOptions={{dateStyle: 'full', timeStyle: 'medium'}} maxUnits={args.maxUnits}/></div>

        <div class="font-medium">Future date:</div>
        <div><FormatRelativeDate date={oneWeekLater} live={args.live} showActualDate={args.showActualDate} maxUnits={args.maxUnits}/></div>
      </div>
      <p class="text-sm text-muted-foreground mt-4">
        Hover over the info icons (ⓘ) to see the actual formatted date. Toggle the "showActualDate" control to hide/show the icons.
      </p>
    </div>
  {/snippet}
</Story>

<Story name="MaxUnits Demo" args={{ maxUnits: 2 }}>
  {#snippet template(args)}
    <LocalizationPicker/>
    <div class="p-4 border rounded-lg">
      <h3 class="font-bold mb-4">MaxUnits Demo - Limiting Display Units</h3>
      <div class="grid grid-cols-2 gap-x-4 gap-y-2 items-center">
        <div class="font-medium">Complex duration (no limit):</div>
        <div><FormatRelativeDate date={complexPast}/></div>

        <div class="font-medium">Complex duration (maxUnits = 1):</div>
        <div><FormatRelativeDate date={complexPast} maxUnits={1}/></div>

        <div class="font-medium">Complex duration (maxUnits = 2):</div>
        <div><FormatRelativeDate date={complexPast} maxUnits={2}/></div>

        <div class="font-medium">Complex duration (maxUnits = 3):</div>
        <div><FormatRelativeDate date={complexPast} maxUnits={3}/></div>

        <div class="font-medium">Moderate duration (no limit):</div>
        <div><FormatRelativeDate date={moderatePast}/></div>

        <div class="font-medium">Moderate duration (maxUnits = 1):</div>
        <div><FormatRelativeDate date={moderatePast} maxUnits={1}/></div>

        <div class="font-medium">Moderate duration (maxUnits = 2):</div>
        <div><FormatRelativeDate date={moderatePast} maxUnits={2}/></div>

        <div class="font-medium">Interactive control:</div>
        <div><FormatRelativeDate date={complexPast} maxUnits={args.maxUnits}/></div>
      </div>
      <p class="text-sm text-muted-foreground mt-4">
        Use the "maxUnits" control above to limit how many time units are displayed. For example, "7 days, 10 hours, 17 minutes, 30 seconds" with maxUnits=2 becomes "7 days, 10 hours".
      </p>
    </div>
  {/snippet}
</Story>
