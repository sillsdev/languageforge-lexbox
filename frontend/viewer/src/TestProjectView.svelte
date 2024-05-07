<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from './lib/in-memory-api-service';
import {LexboxServices} from './lib/services/service-provider';

window.lexbox.ServiceProvider.setService(LexboxServices.LexboxApi, new InMemoryApiService());
</script>
<ProjectView isConnected/>
