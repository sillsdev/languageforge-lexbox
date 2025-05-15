<script lang="ts">
  import { run } from 'svelte/legacy';

  import 'viewer/component';
  import { LfClassicLexboxApi } from './lfClassicLexboxApi';
  import 'viewer/service-declaration';
  import type { PageData } from './$types';
  import t from '$lib/i18n';

  interface Props {
    data: PageData;
  }

  let { data }: Props = $props();
  let project = $derived(data.project);

  const serviceProvider = window.lexbox.ServiceProvider;
  let service = $derived(new LfClassicLexboxApi($project.code));
</script>

{#if service}
  {#key service}
    <lexbox-svelte projectName={$project.name} about={$t('viewer.about')} api={service}></lexbox-svelte>
  {/key}
{/if}
