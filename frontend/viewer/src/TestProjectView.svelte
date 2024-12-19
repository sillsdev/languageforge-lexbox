<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from '$lib/in-memory-api-service';
import {DotnetService} from '$lib/dotnet-types';

const inMemoryLexboxApi = new InMemoryApiService();
window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, inMemoryLexboxApi);
</script>
<ProjectView projectName={inMemoryLexboxApi.projectName} isConnected />
