<script lang="ts">
  import {initProjectContext} from './project-context.svelte';
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {ResourceReturn} from 'runed';

  const props: {
    fetchData: () => Promise<string[]>;
    onReady: (resource: ResourceReturn<string[], unknown, true>) => void;
  } = $props();

  const projectContext = initProjectContext({
    api: {
      getPartsOfSpeech: () => Promise.resolve([]),
    } as unknown as IMiniLcmJsInvokable,
    projectName: 'Test Project',
    projectCode: 'test-project',
  });

  const fetchData = () => props.fetchData();
  const resource = projectContext.lazyApiResource([], () => fetchData());
  const notifyReady = () => props.onReady(resource);
  notifyReady();
</script>
