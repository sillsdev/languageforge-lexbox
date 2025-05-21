<script lang="ts">
  import {type IRichMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '../ui/label';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';

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
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each writingSystems as ws, i (ws.wsId)}
    <Label class="grid gap-y-2 @lg/editor:grid-cols-subgrid col-span-full items-baseline"
           title={`${ws.name} (${ws.wsId})`}>
      <span>{ws.abbreviation}</span>
      <LcmRichTextEditor bind:value={value[ws.wsId]} {readonly}
                         onchange={() => onchange?.(ws.wsId, value[ws.wsId], value)}/>
    </Label>
  {/each}
</div>
