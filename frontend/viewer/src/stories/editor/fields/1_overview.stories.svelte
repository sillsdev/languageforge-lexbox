<script module lang="ts">
  import {MultiSelect, MultiWsInput, Select, WsInput, RichWsInput, RichMultiWsInput} from '$lib/components/field-editors';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import * as Editor from '$lib/components/editor';
  import {writingSystems as demoWritingSystems} from '$project/demo/demo-entry-data';
  import {inputVariants} from '$lib/components/ui/input/input.svelte';

  const { Story } = defineMeta({
    title: 'editor/fields/overview',
  });

  const ws1 = demoWritingSystems.analysis[0];
  const ws2 = demoWritingSystems.analysis[1];
  const ws3 = demoWritingSystems.vernacular[0];
  const writingSystems2 = [ws1, ws2];
  const writingSystems3 = [ws1, ws2, ws3];
</script>

<Story name="All fields">
  {#snippet template()}
    <Editor.Root>
      <Editor.Grid>
        <div class="col-span-full font-bold">Single WS Input</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body>
            <WsInput
              value="Value"
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body>
            <WsInput
              readonly
              value="Value"
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Empty" />
          <Editor.Field.Body>
            <WsInput
              value={undefined}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly / Empty" />
          <Editor.Field.Body>
            <WsInput
              readonly
              value={undefined}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Rich WS Input</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body>
            <RichWsInput
              value={{spans: [{ws: ws1.wsId, text: 'Value '}, {ws: ws2.wsId, text: 'Value'}]}}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body>
            <RichWsInput
              readonly
              value={{spans: [{ws: ws1.wsId, text: 'Value '}, {ws: ws2.wsId, text: 'Value'}]}}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Empty" />
          <Editor.Field.Body>
            <RichWsInput
              value={undefined}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly / Empty" />
          <Editor.Field.Body>
            <RichWsInput
              readonly
              value={undefined}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Multi-line" />
          <Editor.Field.Body>
            <RichWsInput
              value={{spans: [{ws: ws1.wsId, text: 'Value\nValue '}, {ws: ws2.wsId, text: 'Value'}]}}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly / Multi-line" />
          <Editor.Field.Body>
            <RichWsInput
              readonly
              value={{spans: [{ws: ws1.wsId, text: 'Value\nValue '}, {ws: ws2.wsId, text: 'Value'}]}}
              writingSystem={ws1} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Multi WS Input</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body subGrid>
            <MultiWsInput
              value={{[ws1.wsId]: 'Value'}}
              writingSystems={writingSystems2} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body subGrid>
            <MultiWsInput
              readonly
              value={{[ws1.wsId]: 'Value'}}
              writingSystems={writingSystems2} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Rich Multi WS Input</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body subGrid>
            <RichMultiWsInput
              value={{
                [ws1.wsId]: {spans: [{ws: ws1.wsId, text: 'Value '}, {ws: ws2.wsId, text: 'Value'}]},
                [ws3.wsId]: {spans: [{ws: ws3.wsId, text: 'Value\nValue '}, {ws: ws2.wsId, text: 'Value'}]},
              }}
              writingSystems={writingSystems3} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body subGrid>
            <RichMultiWsInput
              readonly
              value={{
                [ws1.wsId]: {spans: [{ws: ws1.wsId, text: 'Value '}, {ws: ws2.wsId, text: 'Value'}]},
                [ws3.wsId]: {spans: [{ws: ws3.wsId, text: 'Value\nValue '}, {ws: ws2.wsId, text: 'Value'}]},
              }}
              writingSystems={writingSystems3} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Single Select</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body>
            <Select
              value={{value: 'Noun'}}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]}
            />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body>
            <Select
              readonly
              value={{value: 'Noun'}}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]}
            />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Empty" />
          <Editor.Field.Body>
            <Select
              value={undefined}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]}
            />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly / Empty" />
          <Editor.Field.Body>
            <Select
              readonly
              value={undefined}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]}
            />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Multi Select</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Standard" />
          <Editor.Field.Body>
            <MultiSelect
              values={[{value: 'Noun'}]}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly" />
          <Editor.Field.Body>
            <MultiSelect
              readonly
              values={[{value: 'Noun'}]}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Empty" />
          <Editor.Field.Body>
            <MultiSelect
              values={[]}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Readonly / Empty" />
          <Editor.Field.Body>
            <MultiSelect
              readonly
              values={[]}
              idSelector="value"
              labelSelector="value"
              options={[{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>
  {/snippet}
</Story>

<Story name="Value alignment">
  {#snippet template()}
    <div class="font-bold">Editable</div>
    <div class="grid grid-cols-3 w-96 gap-1">
      <WsInput value="WS Input ... ... ..." writingSystem={ws1} />
      <RichWsInput value={{spans: [{ws: ws1.wsId, text: 'Rich '}, {ws: ws2.wsId, text: ' WS Input ... ... ...'}]}} writingSystem={ws1} />
      <textarea class={inputVariants()}>Text area ... ... ...</textarea>
    </div>
    <div class="font-bold">Readonly</div>
    <div class="grid grid-cols-3 w-96 gap-1">
      <WsInput readonly value="WS Input ... ... ..." writingSystem={ws1} />
      <RichWsInput readonly value={{spans: [{ws: ws1.wsId, text: 'Rich '}, {ws: ws2.wsId, text: ' WS Input ... ... ...'}]}} writingSystem={ws1} />
      <textarea readonly class={inputVariants()}>Text area ... ... ...</textarea>
    </div>
  {/snippet}
</Story>
