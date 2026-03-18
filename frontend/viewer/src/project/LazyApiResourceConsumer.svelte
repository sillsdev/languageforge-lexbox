<script lang="ts">
  import type {ResourceReturn} from 'runed';
  import {useProjectContext} from './project-context.svelte';

  const props: {
    fetchData: () => Promise<string[]>;
    onResource: (res: ResourceReturn<string[], unknown, true>) => void;
  } = $props();

  const projectContext = useProjectContext();
  const resource = projectContext.lazyApiResource([], () => props.fetchData());

  // svelte-ignore state_referenced_locally
  props.onResource(resource);
</script>

<span data-testid="consumer-count">{resource.current.length}</span>
