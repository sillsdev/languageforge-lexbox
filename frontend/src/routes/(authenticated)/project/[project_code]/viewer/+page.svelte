<script lang="ts">
  import 'viewer/component';
  import {LexboxService} from 'viewer/service-provider';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';
  import type {PageData} from './$types';

  export let data: PageData;
  $: project = data.project;

  const serviceProvider = window.lexbox.ServiceProvider;
  let service: LfClassicLexboxApi;
  $: {
    if (serviceProvider) {
      let localService = new LfClassicLexboxApi($project.code);
      serviceProvider.setService(LexboxService.LexboxApi, localService);
      service = localService;
    }
  }

</script>
{#if service}
  {#key service}
    <lexbox-svelte projectName={$project.name}></lexbox-svelte>
  {/key}
{/if}
