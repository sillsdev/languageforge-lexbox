<script lang="ts">
  import 'viewer/component';
  import {LexboxService} from 'viewer/service-provider';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';
  import type {PageData} from './$types';
  import t from '$lib/i18n';
  import { derived } from 'svelte/store';

  export let data: PageData;
  $: project = data.project;

  const about = derived(t, ($t) => $t('viewer.about'));
  const serviceProvider = window.lexbox.ServiceProvider;
  let service: LfClassicLexboxApi;
  $: {
    if (serviceProvider) {
      let localService = new LfClassicLexboxApi($project.code, about);
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
