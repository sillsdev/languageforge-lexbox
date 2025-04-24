<script lang="ts">
  import 'viewer/component';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';
  import 'viewer/service-declaration';
  import type {PageData} from './$types';
  import t from '$lib/i18n';

  export let data: PageData;
  $: project = data.project;
  let service: LfClassicLexboxApi;
  $: service = new LfClassicLexboxApi($project.code);

</script>
{#if service}
  {#key service}
    <lexbox-svelte projectName={$project.name} about={$t('viewer.about')} api={service}></lexbox-svelte>
  {/key}
{/if}
