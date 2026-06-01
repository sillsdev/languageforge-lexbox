<script lang="ts" module>
  // Shared cache symbol so all consumers across mounts in the same
  // projectContext share the same WrapperService instance — mirroring how
  // PartOfSpeechService is cached in `getOrAdd`.
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

  // Mirror DictionaryEntry: read wrapper.transformed inside a $derived,
  // then look up an entry by key (the exact shape that broke live).
  const found = $derived(wrapper.transformed.find((item) => item.value === props.lookupKey));
</script>

<span data-testid="consumer-found">{found?.upper ?? 'NONE'}</span>
<span data-testid="consumer-count">{wrapper.transformed.length}</span>
