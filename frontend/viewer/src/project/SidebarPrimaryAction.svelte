<script lang="ts" module>
  import {useProjectContext} from '$project/project-context.svelte';

  const primaryActionSymbol = Symbol.for('fw-lite-primaryAction');

  export class PrimaryAction {
    //boolean indicates if the sidebar is open or not
    snippet: Snippet<[boolean]> | undefined = $state(undefined);
  }

  export function usePrimaryAction() {
    const projectContext = useProjectContext();
    return projectContext.getOrAdd(primaryActionSymbol, () => new PrimaryAction());
  }
</script>
<script lang="ts">
  import type {Snippet} from 'svelte';

  const primaryAction = usePrimaryAction();
  let {
    children = undefined
  }: {
    children?: Snippet<[boolean]>
  } = $props();
  $effect(() => {
    if (children) {
      primaryAction.snippet = children;
    }
    return () => {
      if (primaryAction.snippet === children)
        primaryAction.snippet = undefined;
    }
  });
</script>
