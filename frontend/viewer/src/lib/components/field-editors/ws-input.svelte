<script lang="ts">
  import type { IWritingSystem } from '$lib/dotnet-types';
  import type { ReadonlyDeep } from 'type-fest';
  import { Input } from '../ui/input';

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: string | undefined;
    readonly?: boolean;
    writingSystem: ReadonlyDeep<IWritingSystem>;
    onchange?: (value: string | undefined) => void;
  } = $props();

  const { readonly = false, writingSystem: ws, onchange } = $derived(constProps);
</script>

<Input
  {readonly}
  bind:value={value}
  title={`${ws.name} (${ws.wsId})`}
  placeholder={ws.abbreviation}
  onchange={() => onchange?.(value)} />
