<script lang="ts">
  import {onDestroy, type Snippet} from 'svelte';
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import {mergeProps} from 'bits-ui';
  import {useRecorderTrigger} from './recorder.svelte';
  import {cn} from '$lib/utils';

  interface Props extends ButtonProps {
    child?: Snippet<[{props: Record<string, unknown>}]>;
    recording?: boolean;
    walkieTalkieMode?: boolean;
  }

  let {ref = $bindable(null), walkieTalkieMode = $bindable(true), child, ...restProps}: Props = $props();

  const recorderApi = useRecorderTrigger();

  let releaseToStop = $state(true);
  let walkedTalkieTimeout: ReturnType<typeof setTimeout> | undefined;
  let waitingForRecorderToStart = $state(false);

  async function onTriggerDown() {
    clearTimeout(walkedTalkieTimeout);

    if (recorderApi.recording) {
      recorderApi.onStopRecording();
    } else {
      if (waitingForRecorderToStart) return; // Prevent multiple starts
      waitingForRecorderToStart = true;
      walkieTalkieMode = true;
      try {
        await recorderApi.onStartRecording();
      } finally {
        waitingForRecorderToStart = false;
      }

      // 400 ms to opt out of walkie-talkie mode i.e. prevent pausing on release/up
      releaseToStop = false;
      walkedTalkieTimeout = setTimeout(() => (releaseToStop = true), 400);
    }
  }

  onDestroy(() => {
    if (walkedTalkieTimeout) {
      clearTimeout(walkedTalkieTimeout);
    }
  });

  function onTriggerUp() {
    if (recorderApi.recording) {
      if (releaseToStop) recorderApi.onStopRecording();
      else walkieTalkieMode = false;
    } else if (waitingForRecorderToStart) {
      // the trigger is already up, so it's too late for walkie-talkie mode
      walkieTalkieMode = false;
    }
  }

  function onPointerDown(event: MouseEvent) {
    if (event.button !== 0) return; // Only handle left mouse button
    void onTriggerDown();
  }

  function onPointerUp(event: MouseEvent) {
    if (event.button !== 0) return; // Only handle left mouse button
    onTriggerUp();
  }

  function onPointerLeave() {
    if (recorderApi.recording) {
      walkieTalkieMode = false; // or could stop recording
    }
  }

  function onKeyDown(event: KeyboardEvent) {
    if ((event.key === 'Enter' || event.key === ' ') && !event.repeat) {
      event.preventDefault();
      void onTriggerDown();
    }
  }

  function onKeyUp(event: KeyboardEvent) {
    if ((event.key === 'Enter' || event.key === ' ') && !event.repeat) {
      event.preventDefault();
      onTriggerUp();
    }
  }

  const defaultProps = $derived({
    class: cn('rounded-full hover:brightness-90 select-none', recorderApi.recording ? '!bg-foreground' : '!bg-red-600'),
    icon: recorderApi.recording ? 'i-mdi-stop' : 'i-mdi-microphone',
    iconProps: {class: cn(recorderApi.recording ? 'text-red-600 size-8' : 'size-7')},
    size: 'xl-icon',
    // events
    onpointerdown: onPointerDown,
    onpointerup: onPointerUp,
    onpointerleave: onPointerLeave,
    onkeydown: onKeyDown,
    onkeyup: onKeyUp,
  } satisfies Props);

  const mergedProps = $derived(mergeProps(defaultProps, restProps));
</script>

{#if child}
  {@render child({props: mergedProps})}
{:else}
  <Button {...mergedProps} />
{/if}
