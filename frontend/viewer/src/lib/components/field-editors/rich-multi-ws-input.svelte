<script lang="ts">
  import {type IRichMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import {Label} from '../ui/label';
  import StompSafeLcmRichTextEditor from '../stomp/stomp-safe-lcm-rich-text-editor.svelte';

  const fieldBodyProps = tryUseFieldBody();
  const labelledBy = fieldBodyProps?.labelId;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: IRichMultiString;
    readonly?: boolean;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
    onchange?: (wsId: string, value: IRichString, values: IRichMultiString) => void;
    autofocus?: boolean;
  } = $props();

  const {
    readonly = false,
    writingSystems,
    onchange,
    autofocus,
  } = $derived(constProps);

  function onRichTextChange(wsId: string) {
    let richString = value[wsId]
    richString?.spans.forEach((span) => span.ws ??= wsId);
    onchange?.(wsId, richString, value);
  }

  const rootId = $props.id();
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each writingSystems as ws, i (ws.wsId)}
    {@const inputId = `${rootId}-${ws.wsId}`}
    {@const labelId = `${inputId}-label`}
    <div class="grid gap-y-2 @lg/editor:grid-cols-subgrid col-span-full items-baseline"
      title={`${ws.name} (${ws.wsId})`}>
      <Label id={labelId} for={inputId}>{ws.abbreviation}</Label>
      <StompSafeLcmRichTextEditor
        bind:value={value[ws.wsId]}
        normalWs={ws.wsId}
        id={inputId}
        aria-labelledby="{labelledBy ?? ''} {labelId}"
        {readonly}
        autofocus={autofocus && (i === 0)}
        onchange={() => onRichTextChange(ws.wsId)}
        aria-label={ws.abbreviation}
        />
    </div>
  {/each}
</div>
