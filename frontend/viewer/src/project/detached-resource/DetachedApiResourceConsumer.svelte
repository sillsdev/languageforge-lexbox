<script lang="ts">
  import type {ResourceReturn} from 'runed';
  import {untrack} from 'svelte';
  import {useProjectContext} from '../project-context.svelte';

  const props: {
    fetchData: () => Promise<string[]>;
    onResource: (res: ResourceReturn<string[], unknown, true>) => void;
  } = $props();

  const projectContext = useProjectContext();
  const resource = projectContext.apiResource([], () => props.fetchData());

  untrack(() => props.onResource(resource));
</script>

<span data-testid="consumer-count">{resource.current.length}</span>
