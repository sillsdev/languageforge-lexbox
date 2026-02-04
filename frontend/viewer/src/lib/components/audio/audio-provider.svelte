<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import * as Recorder from './recorder';
  import type {HTMLAttributes} from 'svelte/elements';
  import {formatDigitalDuration} from '../ui/format/format-duration';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

  type Props = HTMLAttributes<HTMLDivElement> & {
    onFileSelected: (file: File) => void;
    onRecordingComplete: (blob: Blob) => void;
  };

  let {onFileSelected, onRecordingComplete, class: className, ...rest}: Props = $props();

  let recorderContainerElem = $state<HTMLElement>();
  let recording = $state(false);
  let walkieTalkieMode = $state<boolean>(false);
  let duration = $state<number>();
  let fileInputElement = $state<HTMLInputElement>();

  const digitalDuration = $derived(duration ? formatDigitalDuration({milliseconds: duration}) : undefined);

  function selectFile() {
    fileInputElement?.click();
  }

  function handleFileSelection(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    try {
      if (!file) throw new Error('No file selected');
      if (!file.type.startsWith('audio/')) throw new Error('Please select an audio file');
    } catch (error) {
      target.value = '';
      throw error;
    }
    onFileSelected(file);
  }

  const recordingMode = $derived(recording || digitalDuration);
</script>

<div
  class={cn(
    'border-input md:border-4 rounded-lg place-content-center text-center',
    /* todo: uncomment drag-n-drop-ish indicator */
    /*recordingMode || 'border-dashed',*/
    className,
  )}
  {...rest}
>
  <div class="flex flex-col gap-4 h-full w-full items-center md:py-6">
    <div class="grow place-content-center relative w-full">
      <div bind:this={recorderContainerElem} class="absolute inset-0 pointer-events-none"></div>
      <Button
        variant={IsMobile.value ? 'secondary' : 'ghost'}
        icon="i-mdi-folder-open-outline"
        iconProps={{class: 'size-10'}}
        class={cn(
          'flex-col min-w-24 h-24 p-4 text-base font-normal text-muted-foreground',
          recordingMode && 'invisible',
        )}
        onclick={selectFile}
      >
        {#if IsMobile.value}
          {$t`Select file`}
        {:else}
          <!-- todo: {$t`Select or drop file`} -->
          <!-- https://www.shadcn-svelte-extras.com/components/file-drop-zone -->
          {$t`Select file`}
        {/if}
      </Button>
    </div>
    <Recorder.Root
      container={recorderContainerElem}
      onRecordingComplete={(b) => onRecordingComplete(b)}
      bind:recording
      bind:duration
    >
      <div class="flex flex-col items-center gap-2">
        {#if digitalDuration}
          <span>{digitalDuration}</span>
        {:else}
          <span class={cn('text-muted-foreground text-sm mx-4 whitespace-pre-wrap', recording && 'invisible')}>
            {$t`Hold to record or\npress and release to start recording.`}
          </span>
        {/if}
        <Recorder.Trigger autofocus bind:walkieTalkieMode />
      </div>
    </Recorder.Root>
  </div>

  <!--
  Hidden file input.
  Should not be at root level as it might trigger a margin/gap.
  -->
  <input bind:this={fileInputElement} type="file" accept="audio/*" onchange={handleFileSelection} class="hidden" />
</div>
