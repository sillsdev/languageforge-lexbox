<script>
  import ProjectView from './ProjectView.svelte';

</script>
<svelte:options customElement={{ tag: 'lexbox-svelte' }} />

<style global>
  @import './app.postcss';
</style>

<ProjectView isConnected />
