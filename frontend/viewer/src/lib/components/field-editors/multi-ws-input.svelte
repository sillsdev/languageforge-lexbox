<script lang="ts" module>
  export type WsInputNote = {message: string; severity: 'error' | 'warning'};
</script>

<script lang="ts">
  import {type IMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import {Label} from '../ui/label';
  import StompSafeInput from '../stomp/stomp-safe-input.svelte';
  import AudioInput from './audio-input.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {cn} from '$lib/utils';

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
    notes?: Partial<Record<string, WsInputNote>>;
  } = $props();

  const {readonly = false, writingSystems, onchange, autofocus, notes} = $derived(constProps);
  let visibleWritingSystems = $derived(supportsAudio ? writingSystems : writingSystems.filter((ws) => !ws.isAudio));

  const rootId = $props.id();
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each visibleWritingSystems as ws, i (ws.wsId)}
    {@const inputId = `${rootId}-${ws.wsId}`}
    {@const labelId = `${inputId}-label`}
    {@const note = notes?.[ws.wsId]}
    {@const noteId = `${inputId}-note`}
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
          aria-describedby={note ? noteId : undefined}
          aria-invalid={note?.severity === 'error' || undefined}
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
      {#if note}
        <p
          id={noteId}
          class={cn(
            'text-sm @lg/editor:col-start-2',
            note.severity === 'error' ? 'text-destructive' : 'text-amber-700 dark:text-amber-300',
          )}
        >
          {note.message}
        </p>
      {/if}
    </div>
  {/each}
</div>
