<script lang="ts">
  import { run } from 'svelte/legacy';

  import 'viewer/component';
  import { LfClassicLexboxApi } from './lfClassicLexboxApi';
  import 'viewer/service-declaration';
  import { DotnetService } from 'viewer/mini-lcm-api';
  import type { PageData } from './$types';
  import t from '$lib/i18n';

  interface Props {
    data: PageData;
  }

  let { data }: Props = $props();
  let project = $derived(data.project);

  const serviceProvider = window.lexbox.ServiceProvider;
  let service: LfClassicLexboxApi | undefined = $state();
  run(() => {
    if (serviceProvider) {
      let localService = new LfClassicLexboxApi($project.code);
      serviceProvider.setService(DotnetService.MiniLcmApi, localService);
      service = localService;
    }
  });
</script>

{#if service}
  {#key service}
    <lexbox-svelte projectName={$project.name} about={$t('viewer.about')}></lexbox-svelte>
  {/key}
{/if}
