<script lang="ts">
  import type {Snippet} from 'svelte';
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {IReadFileResponseJs} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IReadFileResponseJs';
  import {initImageService} from '$lib/entry-editor/field-editors/image-service.svelte';

  type GetFileStream = (mediaUri: string, downloadIfMissing: boolean) => Promise<IReadFileResponseJs>;

  let {
    children,
    getFileStream,
  }: {
    children: Snippet;
    getFileStream: GetFileStream;
  } = $props();

  // ImageService only calls getFileStream; a casted partial mock is enough.
  initImageService(() => ({getFileStream}) as IMiniLcmJsInvokable);
</script>

{@render children()}
