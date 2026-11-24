<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
  import StoryState from '../StoryState.svelte';
  import OpenNameDialog from './OpenNameDialog.svelte';
  import OpenCreateProjectDialog from './OpenCreateProjectDialog.svelte';
  import {DotnetService} from '$lib/dotnet-types';
  import {fallbackPlatformFeaturesService, refreshPlatformFeatures} from '$lib/services/platform-features-service';

  // Windows suggests the OS account's name
  window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
    ...fallbackPlatformFeaturesService,
    suggestedAuthorName: () => Promise.resolve('Pat Example'),
  });
  refreshPlatformFeatures();
</script>

<Story name="Name dialog: first time">
  <StoryState authorName="">
    <OpenNameDialog />
  </StoryState>
</Story>
<Story name="Name dialog: change">
  <StoryState authorName="Pat">
    <OpenNameDialog fromMenu />
  </StoryState>
</Story>
<Story name="Name dialog: from the menu in dev mode">
  <StoryState channel="dev" authorName="Pat">
    <OpenNameDialog fromMenu />
  </StoryState>
</Story>
<Story name="New project: no name yet">
  <StoryState channel="dev" authorName="">
    <OpenCreateProjectDialog />
  </StoryState>
</Story>
<Story name="New project: name already set">
  <StoryState channel="dev" authorName="Pat">
    <OpenCreateProjectDialog />
  </StoryState>
</Story>
