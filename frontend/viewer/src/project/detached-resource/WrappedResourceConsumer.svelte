<script lang="ts" module>
  // Shared so all consumers in a projectContext get the same cached WrapperService
  // (mirrors how PartOfSpeechService is cached via getOrAdd).
  export const WRAPPER_SYMBOL = Symbol.for('test:wrapper-service');
</script>

<script lang="ts">
  import {useProjectContext} from '../project-context.svelte';
  import {WrapperService} from './wrapper-service.svelte';

  const props: {
    fetchData: () => Promise<string[]>;
    lookupKey: string;
  } = $props();

  const projectContext = useProjectContext();
  const wrapper = projectContext.getOrAdd(WRAPPER_SYMBOL, () => {
    const resource = projectContext.apiResource([], () => props.fetchData());
    return new WrapperService(resource);
  });

  // Mirrors DictionaryEntry: read wrapper.transformed in a $derived, then look up by key.
  const found = $derived(wrapper.transformed.find((item) => item.value === props.lookupKey));
</script>

<span data-testid="consumer-found">{found?.upper ?? 'NONE'}</span>
<span data-testid="consumer-count">{wrapper.transformed.length}</span>
