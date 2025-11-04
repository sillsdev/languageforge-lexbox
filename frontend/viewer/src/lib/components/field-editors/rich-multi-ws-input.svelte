<script lang="ts">
  import {type IRichMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import {Label} from '../ui/label';
  import StompSafeLcmRichTextEditor from '../stomp/stomp-safe-lcm-rich-text-editor.svelte';
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import {useProjectContext} from '$project/project-context.svelte';

  const projectContext = useProjectContext();
  const supportsAudio = $derived(projectContext?.features.audio);
  const fieldBodyProps = tryUseFieldBody();
  const labelledBy = fieldBodyProps?.labelId;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: IRichMultiString;
    readonly?: boolean;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
    onchange?: (wsId: string, value: IRichString | undefined, values: IRichMultiString) => void;
    autofocus?: boolean;
  } = $props();

  const {
    readonly = false,
    writingSystems,
    onchange,
    autofocus,
  } = $derived(constProps);
  let visibleWritingSystems = $derived(supportsAudio ? writingSystems : writingSystems.filter(ws => !ws.isAudio));

  function onRichTextChange(wsId: string) {
    let richString = value[wsId]
    richString?.spans.forEach((span) => span.ws ??= wsId);
    onchange?.(wsId, richString, value);
  }

  function getAudioId(richString: IRichString | undefined): string | undefined {
    return richString?.spans[0].text;
  }

  function setAudioId(audioId: string | undefined, wsId: string) {
    let richString = audioId === undefined ? undefined : {spans: [{text: audioId ?? '', ws: wsId}]};
    if (richString) {
      value[wsId] = richString;
    } else {
      delete value[wsId];
    }
    onchange?.(wsId, richString, value);
  }

  const rootId = $props.id();
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each visibleWritingSystems as ws, i (ws.wsId)}
    {@const inputId = `${rootId}-${ws.wsId}`}
    {@const labelId = `${inputId}-label`}
    <div class="grid gap-y-2 @lg/editor:grid-cols-subgrid col-span-full items-baseline"
      title={`${ws.name} (${ws.wsId})`}>
      <Label id={labelId} for={inputId}>{ws.abbreviation}</Label>
      {#if !ws.isAudio}
        <StompSafeLcmRichTextEditor
          bind:value={value[ws.wsId]}
          normalWs={ws.wsId}
          id={inputId}
          aria-labelledby="{labelledBy ?? ''} {labelId}"
          {readonly}
          autofocus={autofocus && (i === 0)}
          autocapitalize="off"
          onchange={() => onRichTextChange(ws.wsId)}
          aria-label={ws.abbreviation}
        />
      {:else}
        <AudioInput
          bind:audioId={() => getAudioId(value[ws.wsId]), audioId => setAudioId(audioId, ws.wsId)}
          wsLabel={ws.abbreviation}
          {readonly} />
      {/if}
    </div>
  {/each}
</div>
