<script lang="ts">
  import 'viewer/component';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';
  import 'viewer/service-declaration';
  import type {PageData} from './$types';
  import t from '$lib/i18n';

  export let data: PageData;
  $: project = data.project;

  const serviceProvider = window.lexbox.ServiceProvider;
  let service: LfClassicLexboxApi;
  $: {
    if (serviceProvider) {
      let localService = new LfClassicLexboxApi($project.code);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      serviceProvider.setService('MiniLcmApi' as any, localService);
      service = localService;
    }
  }

</script>
{#if service}
  {#key service}
    <lexbox-svelte projectName={$project.name} about={$t('viewer.about')}></lexbox-svelte>
  {/key}
{/if}
