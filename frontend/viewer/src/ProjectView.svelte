<script lang="ts">
  import ShadcnProjectView from './ShadcnProjectView.svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  const {
    onloaded = () => {},
    ...rest
  }: {
    onloaded?: (loaded: boolean) => void;
  } & HTMLAttributes<HTMLDivElement> = $props();
</script>

<ShadcnProjectView {onloaded} {...rest}/>
