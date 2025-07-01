<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {Toaster} from '$lib/components/ui/sonner';

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    title: 'notifications/examples',
    component: Toaster,
  });
</script>

<script lang="ts">
  import {AppNotification} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';

  function triggerNotificationWithLargeDetail() {
    let detail = '';
    for (let i = 0; i < 100; i++) {
      if (i % 10 === 0) detail += '='.repeat(20);
      detail += `This is line ${i + 1} of the detail\n`;
    }
    AppNotification.display('This is a notification with a large detail', 'info', duration, detail);
  }

  let duration = $state<number>();
</script>

<div class="flex flex-col gap-4">
  <h1 class="text-2xl font-bold">Notifications</h1>
  <Label>
    Duration (ms):
    <Input type="number" bind:value={duration} />
    <span class="text-sm text-muted-foreground">Only applies to non-error and non-action notifications</span>
  </Label>
  <div class="flex flex-wrap gap-2">
    <Button onclick={() => {throw new Error('This is a test exception');}}>Throw Exception</Button>
    <Button onclick={() => new Promise(() => { throw new Error('This is a test exception');})}>Throw Exception Async</Button>
    <Button onclick={() => AppNotification.display('This is a simple notification', 'info', duration)}>Simple
      Notification
    </Button>
    <Button onclick={() => AppNotification.displayAction('This is a notification with an action', 'info', {
      label: 'Action',
      callback: () => alert('Action clicked')
    })}>
      Notification with action
    </Button>
    <Button onclick={() => triggerNotificationWithLargeDetail()}>
      Notification with a large detail
    </Button>
  </div>
</div>

<Story name="Notifications" />
