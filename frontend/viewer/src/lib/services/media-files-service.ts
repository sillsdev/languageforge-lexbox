import type {IMediaFilesServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMediaFilesServiceJsInvokable';
import type {IHarmonyResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IHarmonyResource';
import type {ILocalResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/ILocalResource';
import type {IRemoteResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IRemoteResource';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';

export function useMediaFilesService() {
  const projectContext = useProjectContext();
  if (!projectContext.mediaFilesService) {
    throw new Error('MediaFilesService not available in the current project context');
  }
  return new MediaFilesService(projectContext);
}

export class MediaFilesService {
  #projectContext: ProjectContext;
  get mediaFilesApi(): IMediaFilesServiceJsInvokable {
    if (!this.#projectContext.mediaFilesService) {
      throw new Error('MediaFilesService not available in the current project context');
    }
    return this.#projectContext.mediaFilesService;
  }

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
  }
  allResources() {
    return this.mediaFilesApi.allResources();
  }
  resourcesPendingDownload() {
    return this.mediaFilesApi.resourcesPendingDownload();
  }
  resourcesPendingUpload() {
    return this.mediaFilesApi.resourcesPendingUpload();
  }
  downloadAllResources() {
    return this.mediaFilesApi.downloadAllResources();
  }
  uploadAllResources() {
    return this.mediaFilesApi.uploadAllResources();
  }
  downloadResources(resources: string[]) {
    return this.mediaFilesApi.downloadResources(resources);
  }
  uploadResources(resources: string[]) {
    return this.mediaFilesApi.uploadResources(resources);
  }
  getFileMetadata(id: string) {
    return this.mediaFilesApi.getFileMetadata(id);
  }
}
