<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {expect, fn, userEvent, within} from 'storybook/test';
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

  const longCaptionPicture: IPicture = {
    ...picture,
    id: 'story-picture-long-caption',
    caption: {
      en: {
        spans: [{
          text: 'A very long caption that should wrap and then clamp after two lines so we can see the ellipsis treatment on thumbnail pictures',
          ws: 'en',
        }],
      },
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

  const houseSvg = demoPictureSvgs[demoPictureMediaUris.house1];

  function loadingGetFileStream() {
    return new Promise<IReadFileResponseJs>(() => {});
  }

  function notDownloadedGetFileStream(_uri: string, downloadIfMissing: boolean): Promise<IReadFileResponseJs> {
    return Promise.resolve(downloadIfMissing ? successResponse(houseSvg) : {result: ReadFileResult.NotFound});
  }

  function loadedGetFileStream(): Promise<IReadFileResponseJs> {
    return Promise.resolve(successResponse(houseSvg));
  }

  function notFoundGetFileStream(): Promise<IReadFileResponseJs> {
    return Promise.resolve({result: ReadFileResult.NotFound});
  }

  function offlineGetFileStream(): Promise<IReadFileResponseJs> {
    return Promise.resolve({result: ReadFileResult.Offline});
  }

  function unknownGetFileStream(): Promise<IReadFileResponseJs> {
    return Promise.resolve({result: ReadFileResult.Error});
  }

  const actionHandlers = {
    onView: fn(),
    onEdit: fn(),
    onDownload: fn(),
    onDelete: fn(),
  };

  const {Story} = defineMeta({
    component: PictureImage,
    parameters: fwliteStoryParameters({resizable: false}),
    argTypes: {
      size: {control: {type: 'select'}, options: ['thumbnail', 'full']},
      showCaption: {control: {type: 'boolean'}},
      readonly: {control: {type: 'boolean'}},
      busy: {control: {type: 'boolean'}},
    },
    args: {
      picture,
      showCaption: true,
      size: 'thumbnail' as const,
      readonly: false,
      busy: false,
    },
  });
</script>

<script lang="ts">
  import type {ComponentProps} from 'svelte';

  type PictureArgs = ComponentProps<typeof PictureImage>;
</script>

<!-- Load states -->

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

<!-- Parameter variants (loaded image) -->

<Story name="Full size">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <div class="bg-muted/30 flex h-96 w-full items-center justify-center p-4">
        <PictureImage {...args} size="full" />
      </div>
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Without caption">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} showCaption={false} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Long caption">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} picture={longCaptionPicture} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story
  name="With actions menu"
  args={actionHandlers}
  play={async ({canvasElement, args}) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByRole('button', {name: 'Picture actions'}));
    // Menu content portals to the document root.
    const doc = within(document.documentElement);
    await userEvent.click(await doc.findByRole('menuitem', {name: 'Edit'}));
    await expect(args.onEdit).toHaveBeenCalledOnce();
  }}
>
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="View only" args={{onView: actionHandlers.onView}}>
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story name="Busy">
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} {...actionHandlers} busy />
    </PictureImageHarness>
  {/snippet}
</Story>

<Story
  name="Readonly"
  play={async ({canvasElement}) => {
    const canvas = within(canvasElement);
    await expect(canvas.findByRole('button', {name: 'View Picture'})).resolves.toBeInTheDocument();
    await userEvent.click(await canvas.findByRole('button', {name: 'Picture actions'}));
    const doc = within(document.documentElement);
    await expect(doc.findByRole('menuitem', {name: 'Download'})).resolves.toBeInTheDocument();
    await expect(doc.queryByRole('menuitem', {name: 'Edit'})).toBeNull();
    await expect(doc.queryByRole('menuitem', {name: 'Delete'})).toBeNull();
  }}
>
  {#snippet template(args: PictureArgs)}
    <PictureImageHarness getFileStream={loadedGetFileStream}>
      <PictureImage {...args} {...actionHandlers} readonly />
    </PictureImageHarness>
  {/snippet}
</Story>
