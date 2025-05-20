<script lang="ts">
  import ShadcnProjectView from './ShadcnProjectView.svelte';

  const {
    onloaded,
    isConnected,
    showHomeButton = true,
    about = undefined,
  }: {
    onloaded: (loaded: boolean) => void;
    about?: string | undefined;
    isConnected: boolean;
    showHomeButton?: boolean;
  } = $props();
</script>

<ShadcnProjectView {onloaded} {about} {isConnected} {showHomeButton}/>
