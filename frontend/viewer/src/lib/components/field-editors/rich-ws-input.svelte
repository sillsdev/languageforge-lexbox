<script lang="ts">
  import type { IWritingSystem } from '$lib/dotnet-types';
  import type { ReadonlyDeep } from 'type-fest';
  import LcmRichTextEditor from '../lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import type {IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';

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

  const { readonly = false, writingSystem: ws, onchange, autofocus } = $derived(constProps);


  function onRichTextChange() {
    value?.spans.forEach((span) => span.ws ??= ws.wsId);
    onchange?.(value);
  }
</script>

<LcmRichTextEditor
  bind:value={value}
  onchange={onRichTextChange}
  {readonly}
  title={`${ws.name} (${ws.wsId})`}
  {autofocus}
  placeholder={ws.abbreviation} />
