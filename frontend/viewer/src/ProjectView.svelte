<script lang="ts">
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

<SvelteUxProjectView on:loaded={(e) => onloaded(e.detail)} {projectName} {about} {isConnected} {showHomeButton} />
