<script lang="ts">
  import {useWrapperService} from './wrapper-service.svelte';

  const props: {
    fetchData: () => Promise<string[]>;
    lookupKey: string;
  } = $props();

  const wrapper = useWrapperService(() => props.fetchData());

  // Mirrors DictionaryEntry: read wrapper.transformed in a $derived, then look up by key.
  const found = $derived(wrapper.transformed.find((item) => item.value === props.lookupKey));
</script>

<span data-testid="consumer-found">{found?.derived ?? 'NONE'}</span>
<span data-testid="consumer-count">{wrapper.transformed.length}</span>
