<script lang="ts">
  import {type IMultiString, type IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '../ui/label';
  import {Input} from '../ui/input';

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: IMultiString;
    readonly?: boolean;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
    onchange?: (wsId: string, value: string, values: IMultiString) => void;
  } = $props();

  const {
    readonly = false,
    writingSystems,
    onchange,
  } = $derived(constProps);
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each writingSystems as ws (ws.wsId)}
    <Label class="grid gap-y-2 @lg/editor:grid-cols-subgrid col-span-full items-baseline" title={`${ws.name} (${ws.wsId})`}>
      <span>{ws.abbreviation}</span>
      <Input bind:value={value[ws.wsId]} {readonly} onchange={() => onchange?.(ws.wsId, value[ws.wsId], value)} />
    </Label>
  {/each}
</div>
