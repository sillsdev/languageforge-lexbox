<script lang="ts">
  import 'viewer/component';
  import {type LexboxServiceProvider} from 'viewer/service-provider';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';
  import type {PageData} from './$types';

  export let data: PageData;

  const serviceProvider: LexboxServiceProvider = window.lexbox.ServiceProvider;
  let service: LfClassicLexboxApi;
  $: {
    if (serviceProvider) {
      let localService = new LfClassicLexboxApi(data.code);
      serviceProvider.setService('LexboxApi', localService);
      service = localService;
    }
  }

</script>
{#if service}
  {#key service}
    <lexbox-svelte></lexbox-svelte>
  {/key}
{/if}
