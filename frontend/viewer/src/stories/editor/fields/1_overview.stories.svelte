<script module lang="ts">
  import {MultiSelect, MultiWsInput, Select, WsInput, RichWsInput, RichMultiWsInput} from '$lib/components/field-editors';
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import * as Editor from '$lib/components/editor';
  import {writingSystems as demoWritingSystems} from '$project/demo/demo-entry-data';
  import {inputVariants} from '$lib/components/ui/input/input.svelte';
  import {Input} from '$lib/components/ui/input';
  import {Button} from '$lib/components/ui/button';
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import type {Snippet} from 'svelte';

  const { Story } = defineMeta({
    title: 'editor/fields/overview',
  });

  const ws1 = demoWritingSystems.analysis[0];
  const ws2 = demoWritingSystems.analysis[1];
  const ws3 = demoWritingSystems.vernacular[0];
  const writingSystems2 = [ws1, ws2];
  const writingSystems3 = [ws1, ws2, ws3];

  const posOptions = [{value: 'Noun'}, {value: 'Verb'}, {value: 'Adjective'}, {value: 'Adverb'}];
  const richValue = {spans: [{ws: ws1.wsId, text: 'Value '}, {ws: ws2.wsId, text: 'Value'}]};
</script>

{#snippet matrixRow(name: string, field: Snippet<[{readonly: boolean; empty: boolean}]>, disabled?: Snippet)}
  <div class="col-span-full font-bold mt-4 border-b">{name}</div>
  {#each [false, true] as empty (empty)}
    <div class="text-muted-foreground text-sm self-center">{empty ? 'Empty' : 'Filled'}</div>
    {#each [false, true] as readonly (readonly)}
      <Editor.Root>
        <Editor.Grid class="grid-cols-1!">
          <Editor.Field.Root>
            <Editor.Field.Body subGrid>
              {@render field({readonly, empty})}
            </Editor.Field.Body>
          </Editor.Field.Root>
        </Editor.Grid>
      </Editor.Root>
    {/each}
    <div class="text-muted-foreground text-sm self-center">
      {#if disabled && !empty}{@render disabled()}{:else if !disabled}n/a{/if}
    </div>
  {/each}
{/snippet}

{#snippet wsInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <WsInput {readonly} value={empty ? undefined : 'Value'} writingSystem={ws1} />
{/snippet}
{#snippet richWsInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <RichWsInput {readonly} value={empty ? undefined : richValue} writingSystem={ws1} />
{/snippet}
{#snippet multiWsInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <MultiWsInput {readonly} value={empty ? {} : {[ws1.wsId]: 'Value'}} writingSystems={writingSystems2} />
{/snippet}
{#snippet richMultiWsInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <RichMultiWsInput {readonly} value={empty ? {} : {[ws1.wsId]: richValue}} writingSystems={writingSystems2} />
{/snippet}
{#snippet select({readonly, empty}: {readonly: boolean; empty: boolean})}
  <Select {readonly} value={empty ? undefined : posOptions[0]} idSelector="value" labelSelector="value" options={posOptions} />
{/snippet}
{#snippet multiSelect({readonly, empty}: {readonly: boolean; empty: boolean})}
  <MultiSelect {readonly} values={empty ? [] : posOptions.slice(0, 2)} idSelector="value" labelSelector="value" options={posOptions} />
{/snippet}
{#snippet audioInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <AudioInput {readonly} audioId={empty ? undefined : 'demo-audio'} />
{/snippet}
{#snippet nativeInput({readonly, empty}: {readonly: boolean; empty: boolean})}
  <Input {readonly} value={empty ? '' : 'Value'} />
{/snippet}
{#snippet nativeInputDisabled()}
  <Input disabled value="Value" />
{/snippet}
{#snippet outlineButton({readonly}: {readonly: boolean; empty: boolean})}
  <Button variant="outline" class="w-full" disabled={readonly}>Value</Button>
{/snippet}
{#snippet outlineButtonDisabled()}
  <Button variant="outline" class="w-full" disabled>Value</Button>
{/snippet}

<Story name="State matrix" parameters={{fwlite: {resizable: false}}}>
  {#snippet template()}
    <div class="grid grid-cols-[auto_repeat(3,minmax(0,1fr))] gap-x-4 gap-y-2 items-start p-2">
      <div></div>
      <div class="font-bold">Editable</div>
      <div class="font-bold">Read-only</div>
      <div class="font-bold">Disabled <span class="font-normal text-muted-foreground">(field editors have no disabled prop)</span></div>
      {@render matrixRow('Native Input (reference)', nativeInput, nativeInputDisabled)}
      {@render matrixRow('Outline Button (reference, read-only = disabled)', outlineButton, outlineButtonDisabled)}
      {@render matrixRow('WS Input', wsInput)}
      {@render matrixRow('Rich WS Input', richWsInput)}
      {@render matrixRow('Multi WS Input', multiWsInput)}
      {@render matrixRow('Rich Multi WS Input', richMultiWsInput)}
      {@render matrixRow('Select', select)}
      {@render matrixRow('Multi Select', multiSelect)}
      {@render matrixRow('Audio Input', audioInput)}
    </div>
  {/snippet}
</Story>

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
