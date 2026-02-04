<script lang="ts">
  import type {IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import StompSafeInput from '../stomp/stomp-safe-input.svelte';

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

  const {readonly = false, writingSystem: ws, onchange} = $derived(constProps);
</script>

<StompSafeInput
  {readonly}
  bind:value
  title={`${ws.name} (${ws.wsId})`}
  placeholder={ws.abbreviation}
  {autofocus}
  autocapitalize="off"
  aria-labelledby={labelledBy}
  onchange={() => onchange?.(value)}
/>
