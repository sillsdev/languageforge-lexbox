<script lang="ts">
  import ShadcnProjectView from './ShadcnProjectView.svelte';

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

<ShadcnProjectView {onloaded} {projectName} {about} {isConnected} {showHomeButton}/>
