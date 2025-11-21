<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import NotificationOutlet from '$lib/notifications/NotificationOutlet.svelte';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    component: NotificationOutlet,
  });
</script>

<script lang="ts">
  import {AppNotification, pickDuration} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {Dialog, DialogTrigger} from '$lib/components/ui/dialog';
  import DialogContent from '$lib/components/ui/dialog/dialog-content.svelte';
  import {delay} from '$lib/utils/time';

  function triggerNotificationWithLargeDetail() {
    let description = '';
    for (let i = 0; i < 100; i++) {
      if (i % 10 === 0) description += '='.repeat(20);
      description += `This is line ${i + 1} of the detail\n`;
    }
    AppNotification.display(`This is a notification with a large detail. Duration: ${currDuration()}`, { type: 'info', timeout: durationMs, description });
  }

  function triggerPromiseNotification() {
    const resolveTime = 3000;
    AppNotification.promise(delay(resolveTime).then(() => 42),
      {
        loading: `Waiting for promise... (${resolveTime / 1000}s)`,
        success: (r: number) => `Resolved with ${r} (Duration: ${currDuration()})`,
        timeout: durationMs,
      },
    );
  }

  let duration = $state<number | undefined>(3);
  const durationMs = $derived(duration ? duration * 1000 : undefined);

  function currDuration(): string {
    if (durationMs === undefined) return 'Permanent';
    return `${pickDuration(durationMs) / 1000}s`;
  }
</script>

{#snippet notificationControls()}
  <Label class="flex flex-col gap-1">
    Duration (s):
    <div class="flex gap-2">
      <Input type="number" min="2" step="0.5" bind:value={duration} />
      <Button size="sm" onclick={() => duration = undefined}>Clear</Button>
    </div>
    <span class="text-sm text-muted-foreground">
      Exception and action notifications ignore this duration setting. <br />
      The notification service enforces a minimum duration of 3s. <br />
      Copying to clipboard is not supported in the Storybook iframe.
    </span>
  </Label>
  <div class="flex flex-wrap gap-2">
    <Button onclick={() => AppNotification.display(`This is a simple notification. Duration: ${currDuration()}`, { timeout: durationMs })}>
      Plain / Default
    </Button>
    <Button onclick={() => AppNotification.display(`This is a simple notification. Duration: ${currDuration()}`, { type: 'info', timeout: durationMs })}>
      Info
    </Button>
    <Button onclick={() => AppNotification.display(`This is a simple notification. Duration: ${currDuration()}`, { type: 'success', timeout: durationMs })}>
      Success
    </Button>
    <Button onclick={() => AppNotification.display(`This is a simple notification. Duration: ${currDuration()}`, { type: 'warning', timeout: durationMs })}>
      Warning
    </Button>
    <Button variant="destructive" onclick={() => AppNotification.display(`This is a simple notification. Duration: ${currDuration()}`, { type: 'error', timeout: durationMs })}>
      Error
    </Button>
    <Button onclick={() => triggerPromiseNotification()}>
      Promise
    </Button>
  </div>
  <div class="flex flex-wrap gap-2">
    <Button variant="destructive" onclick={() => {throw new Error('This is a test exception');}}>Throw Exception</Button>
    <Button variant="destructive" onclick={() => new Promise(() => { throw new Error('This is a test exception');})}>Throw Exception Async</Button>
    <Button onclick={() => AppNotification.displayAction('This is a notification with an action', {
      label: 'Action',
      callback: () => window.alert('Action clicked'),
    })}>
      Notification with action
    </Button>
    <Button onclick={() => triggerNotificationWithLargeDetail()}>
      Notification with a large detail
    </Button>
  </div>
{/snippet}

<div class="flex flex-col gap-4">
  <div class="flex items-center justify-between">
    <h1 class="text-2xl font-bold">Notifications</h1>
    <Dialog>
      <DialogContent class="flex flex-col">
        {@render notificationControls()}
      </DialogContent>
      <DialogTrigger>
        {#snippet child({props})}
          <Button {...props}>Dialog</Button>
        {/snippet}
      </DialogTrigger>
    </Dialog>
  </div>
  {@render notificationControls()}
</div>

<Story name="Notifications" />
