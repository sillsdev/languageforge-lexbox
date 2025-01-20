<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from '$lib/in-memory-api-service';
import {DotnetService} from '$lib/dotnet-types';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import ProjectLoader from './ProjectLoader.svelte';

const inMemoryLexboxApi = new InMemoryApiService();
window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, inMemoryLexboxApi);
window.lexbox.ServiceProvider.setService(DotnetService.FwLiteConfig, {
  appVersion: 'test-project',
  feedbackUrl: '',
  os: FwLitePlatform.Web,
  useDevAssets: true,
});
const projectName = inMemoryLexboxApi.projectName;
</script>

<ProjectLoader {projectName} let:onProjectLoaded>
  <ProjectView {projectName} isConnected on:loaded={e => onProjectLoaded(e.detail)}></ProjectView>
</ProjectLoader>
