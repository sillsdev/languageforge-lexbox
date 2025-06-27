<script lang="ts">
  import type { IWritingSystem } from '$lib/dotnet-types';
  import type { ReadonlyDeep } from 'type-fest';
  import { Input } from '../ui/input';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';

  const fieldBodyProps = tryUseFieldBody();
  const labelledBy = fieldBodyProps?.labelId;

  let {
    value = $bindable(),
    autofocus,
    ...constProps
  }: {
    value: string | undefined;
    readonly?: boolean;
    writingSystem: ReadonlyDeep<IWritingSystem>;
    onchange?: (value: string | undefined) => void;
    autofocus?: boolean;
  } = $props();

  const { readonly = false, writingSystem: ws, onchange } = $derived(constProps);
</script>

<Input
  {readonly}
  bind:value
  title={`${ws.name} (${ws.wsId})`}
  placeholder={ws.abbreviation}
  {autofocus}
  aria-labelledby={labelledBy}
  onchange={() => onchange?.(value)} />
