<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import DevContent, {isDev} from '$lib/layout/DevContent.svelte';
  import ShadcnProjectView, {useShadcn} from './ShadcnProjectView.svelte';
  import SvelteUxProjectView from './SvelteUxProjectView.svelte';

  const {
    onloaded,
    projectName,
    isConnected,
    showHomeButton = true,
    about = undefined,
  }: {
    onloaded: (loaded: boolean) => void;
    about?: string | undefined;
    projectName: string;
    isConnected: boolean;
    showHomeButton?: boolean;
  } = $props();
</script>

{#if $useShadcn || $isDev}
  <div class="contents" class:hidden={!$useShadcn}>
    <ShadcnProjectView {onloaded} {projectName} {about} {isConnected} {showHomeButton} />
  </div>
{/if}

<div class="contents" class:hidden={$useShadcn}>
  <SvelteUxProjectView on:loaded={(e) => onloaded(e.detail)} {projectName} {about} {isConnected} {showHomeButton} />
</div>

<DevContent>
  <Button onclick={() => globalThis.enableShadcn(!$useShadcn)} class="fixed bottom-2 {!$useShadcn ? 'left-2' : 'right-2'}" icon="i-mdi-ab-testing"
  ></Button>
</DevContent>
