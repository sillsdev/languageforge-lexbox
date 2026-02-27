<script lang="ts">
  import type {IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import StompSafeLcmRichTextEditor from '../stomp/stomp-safe-lcm-rich-text-editor.svelte';

  const fieldBodyProps = tryUseFieldBody();
  const labelledBy = fieldBodyProps?.labelId;

  let {
    value = $bindable(),
    ...constProps
  }: {
    value: IRichString | undefined;
    readonly?: boolean;
    writingSystem: ReadonlyDeep<IWritingSystem>;
    onchange?: (value: IRichString | undefined) => void;
    autofocus?: boolean;
  } = $props();

  const {readonly = false, writingSystem: ws, onchange, autofocus} = $derived(constProps);

  function onRichTextChange() {
    value?.spans.forEach((span) => (span.ws ??= ws.wsId));
    onchange?.(value);
  }
</script>

<StompSafeLcmRichTextEditor
  bind:value
  normalWs={ws.wsId}
  onchange={onRichTextChange}
  {readonly}
  title={`${ws.name} (${ws.wsId})`}
  {autofocus}
  placeholder={ws.abbreviation}
  aria-label={ws.abbreviation}
  aria-labelledby={labelledBy}
/>
