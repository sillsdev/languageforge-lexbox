<script lang="ts">
  import {type IMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import {Label} from '../ui/label';
  import StompSafeInput from '../stomp/stomp-safe-input.svelte';
  import AudioInput from './audio-input.svelte';
  import {useProjectContext} from '$project/project-context.svelte';

  const projectContext = useProjectContext();
  const supportsAudio = $derived(projectContext?.features.audio);
  const fieldBodyProps = tryUseFieldBody();
  const labeledBy = fieldBodyProps?.labelId;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: IMultiString;
    readonly?: boolean;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
    onchange?: (wsId: string, value: string, values: IMultiString) => void;
    autofocus?: boolean;
  } = $props();

  const {readonly = false, writingSystems, onchange, autofocus} = $derived(constProps);
  let visibleWritingSystems = $derived(supportsAudio ? writingSystems : writingSystems.filter((ws) => !ws.isAudio));

  const rootId = $props.id();
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each visibleWritingSystems as ws, i (ws.wsId)}
    {@const inputId = `${rootId}-${ws.wsId}`}
    {@const labelId = `${inputId}-label`}
    <div
      class="grid gap-y-2 @lg/editor:grid-cols-subgrid col-span-full items-baseline"
      title={`${ws.name} (${ws.wsId})`}
    >
      <Label id={labelId} for={inputId}>{ws.abbreviation}</Label>
      {#if !ws.isAudio}
        <StompSafeInput
          bind:value={value[ws.wsId]}
          id={inputId}
          aria-labelledby="{labeledBy ?? ''} {labelId}"
          {readonly}
          autofocus={autofocus && i === 0}
          onchange={() => onchange?.(ws.wsId, value[ws.wsId], value)}
        />
      {:else}
        <AudioInput
          bind:audioId={value[ws.wsId]}
          onchange={() => onchange?.(ws.wsId, value[ws.wsId], value)}
          wsLabel={ws.abbreviation}
          {readonly}
        />
      {/if}
    </div>
  {/each}
</div>
