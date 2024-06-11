<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from './lib/in-memory-api-service';
import {LexboxService} from './lib/services/service-provider';

const inMemoryLexboxApi = new InMemoryApiService();
window.lexbox.ServiceProvider.setService(LexboxService.LexboxApi, inMemoryLexboxApi);
</script>
<ProjectView projectName={inMemoryLexboxApi.projectName} isConnected />
