<script module lang="ts">
  import LcmRichTextEditor from '$lib/components/lcm-rich-text-editor/lcm-rich-text-editor.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Checkbox} from '$lib/components/ui/checkbox';
  import {type IRichString} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IRichString';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import type {FwliteStoryParameters} from '../fwl-parameters';

  //matching the character used in FieldWorks
  //https://unicode-explorer.com/c/2028
  const lineSeparator = '\u2028';
  const originalRichString: IRichString = {
    spans: [{text: 'Hello', ws: 'en'}, {text: ' World', ws: 'js'}, {text: ` type ${lineSeparator}script`, ws: 'ts'}],
  };
  const richString: { current: IRichString | undefined } = $state({
    current: originalRichString,
  });

  // More on how to set up stories at: https://storybook.js.org/docs/writing-stories
  const { Story } = defineMeta({
    component: LcmRichTextEditor,
    parameters: {
      fwlite: {
        showValue: true,
        value: () => richString.current
      } satisfies FwliteStoryParameters,
    },
  });

  let readonly = $state(false);
</script>

<Story name="Debug">
  {#snippet template(args)}
    <div class="space-y-4">
      <LcmRichTextEditor label="Rich Text Editor" bind:value={richString.current} {readonly}
        onchange={() => args.value = richString.current = JSON.parse(JSON.stringify($state.snapshot(richString.current)))} />
      <div class="flex gap-2 items-center">
        <Button onclick={() => richString.current = {spans: [{text: 'test', ws: 'en'}]}}>Replace Rich Text</Button>
        <Button onclick={() => richString.current = undefined}>Set undefined</Button>
        <Button onclick={() => richString.current = originalRichString}>Reset</Button>
        <label class="flex items-center gap-2">
          <Checkbox bind:checked={readonly}/> Readonly
        </label>
      </div>
    </div>
  {/snippet}
</Story>
