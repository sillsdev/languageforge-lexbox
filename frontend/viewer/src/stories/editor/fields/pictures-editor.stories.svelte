<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {expect, userEvent, within} from 'storybook/test';
  import type {IPicture} from '$lib/dotnet-types';
  import PicturesEditor from '$lib/entry-editor/field-editors/PicturesEditor.svelte';
  import {allWsEntry} from '$project/demo/demo-entry-data';
  import {fwliteStoryParameters} from '../../fwl-parameters';
  import FieldDecorator from './FieldDecorator.svelte';
  import PicturesEditorHarness from './PicturesEditorHarness.svelte';

  const sense = allWsEntry.senses[0];
  const entryId = sense.entryId;
  const senseId = sense.id;
  function demoPictures() {
    return structuredClone(sense.pictures ?? []);
  }

  // Per-story bindable copies so edits don't mutate shared demo module data.
  let withPictures = $state(demoPictures());
  let emptyPictures = $state([] as IPicture[]);
  let bufferPictures = $state([] as IPicture[]);
  let readonlyWithPictures = $state(demoPictures());
  let readonlyEmptyPictures = $state([] as IPicture[]);

  // Minimal valid 1x1 PNG (the demo API round-trips the bytes; a PNG type also skips conversion).
  const PNG_BYTES = Uint8Array.from(
    atob(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
    ),
    (c) => c.charCodeAt(0),
  );

  const {Story} = defineMeta({
    component: PicturesEditor,
    parameters: fwliteStoryParameters({resizable: false}),
    argTypes: {
      readonly: {control: {type: 'boolean'}},
    },
    args: {
      entryId,
      senseId,
      readonly: false,
    },
    decorators: [
      /* @ts-expect-error Bug in Storybook https://github.com/storybookjs/storybook/issues/29951 */
      () => ({['Component']: FieldDecorator}),
    ],
  });
</script>

<script lang="ts">
  import type {ComponentProps} from 'svelte';

  type PicturesArgs = ComponentProps<typeof PicturesEditor>;
</script>

<Story
  name="With pictures"
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    const loadButton = await canvas.findAllByRole('button', {name: 'Load picture'});
    await userEvent.click(loadButton[0]);
    const img = await canvas.findByRole('img');
    await expect(img.getAttribute('src')).toMatch(/^blob:/);
  }}
>
  {#snippet template(args: PicturesArgs)}
    <PicturesEditorHarness>
      <PicturesEditor bind:pictures={withPictures} entryId={args.entryId} senseId={args.senseId} readonly={args.readonly} />
    </PicturesEditorHarness>
  {/snippet}
</Story>

<Story name="Empty">
  {#snippet template(args: PicturesArgs)}
    <PicturesEditorHarness>
      <PicturesEditor bind:pictures={emptyPictures} entryId={args.entryId} senseId={args.senseId} readonly={false} />
    </PicturesEditorHarness>
  {/snippet}
</Story>

<Story
  name="Readonly with pictures"
  args={{readonly: true}}
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    const loadButtons = await canvas.findAllByRole('button', {name: 'Load picture'});
    await userEvent.click(loadButtons[0]);
    await userEvent.click(await canvas.findByRole('button', {name: 'View Picture'}));
    const doc = within(document.documentElement);
    const viewer = await doc.findByRole('dialog');
    await expect(viewer).toBeInTheDocument();
    await expect(within(viewer).findByRole('heading', {name: /Picture/})).resolves.toBeInTheDocument();
  }}
>
  {#snippet template(args: PicturesArgs)}
    <PicturesEditorHarness>
      <PicturesEditor
        bind:pictures={readonlyWithPictures}
        entryId={args.entryId}
        senseId={args.senseId}
        readonly={args.readonly}
      />
    </PicturesEditorHarness>
  {/snippet}
</Story>

<Story
  name="Buffers new picture until submit"
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    await expect(canvas.queryAllByRole('img')).toHaveLength(0);

    // The file input is visually hidden (pointer-events: none), so drive it directly instead of
    // through userEvent.upload's click: assign files via a DataTransfer and dispatch `change`.
    const input = canvasElement.querySelector('input[type=file]') as HTMLInputElement;
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(new File([PNG_BYTES], 'test.png', {type: 'image/png'}));
    input.files = dataTransfer.files;
    input.dispatchEvent(new Event('change', {bubbles: true}));

    // Dialog opens in "add" mode; the draft preview lives in the portalled dialog, not the field.
    const doc = within(document.documentElement);
    await doc.findByRole('heading', {name: 'Add Picture'});
    await expect(canvas.queryAllByRole('img')).toHaveLength(0);

    // Nothing is uploaded/created until Submit.
    await userEvent.click(await doc.findByRole('button', {name: 'Submit'}));
    const img = await canvas.findByRole('img');
    await expect(img).toHaveAttribute('src', expect.stringMatching(/^blob:/));
  }}
>
  {#snippet template(args: PicturesArgs)}
    <PicturesEditorHarness>
      <PicturesEditor bind:pictures={bufferPictures} entryId={args.entryId} senseId={args.senseId} readonly={false} />
    </PicturesEditorHarness>
  {/snippet}
</Story>

<Story name="Readonly empty" args={{readonly: true}}>
  {#snippet template(args: PicturesArgs)}
    <PicturesEditorHarness>
      <PicturesEditor
        bind:pictures={readonlyEmptyPictures}
        entryId={args.entryId}
        senseId={args.senseId}
        readonly={args.readonly}
      />
    </PicturesEditorHarness>
  {/snippet}
</Story>
