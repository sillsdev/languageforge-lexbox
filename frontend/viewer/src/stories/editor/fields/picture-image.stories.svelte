<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {expect, userEvent, within} from 'storybook/test';
  import type {IPicture} from '$lib/dotnet-types';
  import type {IReadFileResponseJs} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IReadFileResponseJs';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import PictureImage from '$lib/entry-editor/field-editors/PictureImage.svelte';
  import {demoPictureMediaUris, demoPictureSvgs} from '$project/demo/demo-entry-data';
  import {fwliteStoryParameters} from '../../fwl-parameters';
  import PictureImageHarness from './PictureImageHarness.svelte';

  const picture: IPicture = {
    id: 'story-picture-1',
    order: 1,
    mediaUri: demoPictureMediaUris.house1,
    caption: {
      en: {spans: [{text: 'A traditional house', ws: 'en'}]},
    },
  };

  function successResponse(svg: string): IReadFileResponseJs {
    const blob = new Blob([svg], {type: 'image/svg+xml'});
    return {
      result: ReadFileResult.Success,
      fileName: 'demo-picture.svg',
      stream: {
        stream: () => Promise.resolve(blob.stream()),
        arrayBuffer: () => blob.arrayBuffer(),
      },
    };
  }

  const houseSvg = demoPictureSvgs[demoPictureMediaUris.house1]!;

  const loadingGetFileStream = () => new Promise<IReadFileResponseJs>(() => {});

  const notDownloadedGetFileStream = async (_uri: string, downloadIfMissing: boolean) => {
    if (!downloadIfMissing) return {result: ReadFileResult.NotFound};
    return successResponse(houseSvg);
  };

  const loadedGetFileStream = async () => successResponse(houseSvg);

  const notFoundGetFileStream = async () => ({result: ReadFileResult.NotFound});

  const offlineGetFileStream = async () => ({result: ReadFileResult.Offline});

  const unknownGetFileStream = async () => ({result: ReadFileResult.Error});

  const {Story} = defineMeta({
    component: PictureImage,
    parameters: fwliteStoryParameters({resizable: false}),
    args: {
      picture,
      showCaption: true,
      size: 'thumbnail' as const,
    },
  });
</script>

<script lang="ts">
  import type {ComponentProps} from 'svelte';

  type PictureArgs = ComponentProps<typeof PictureImage>;
</script>

<Story name="Loading">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadingGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story
  name="Not downloaded"
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    const loadButton = await canvas.findByRole('button', {name: 'Load picture'});
    await userEvent.click(loadButton);
    const img = await canvas.findByRole('img');
    await expect(img.getAttribute('src')).toMatch(/^blob:/);
  }}
>
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={notDownloadedGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Loaded">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story
  name="Error not found"
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    const loadButton = await canvas.findByRole('button', {name: 'Load picture'});
    await userEvent.click(loadButton);
    await expect(canvas.findByText('Picture not found')).resolves.toBeInTheDocument();
  }}
>
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={notFoundGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Offline">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={offlineGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Unknown error">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={unknownGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>
