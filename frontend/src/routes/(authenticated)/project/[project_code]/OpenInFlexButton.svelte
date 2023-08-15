<script lang="ts">
  import { ProjectTypeIcon } from '$lib/components/ProjectType';
  import { ProjectType } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let projectId: string;
</script>

<a
  class="btn btn-primary whitespace-nowrap open-in-flex"
  href={`/api/integration/openWithFlex?projectId=${projectId}`}
  data-sveltekit-preload-data="off"
  on:click
>
  {$t('project_page.open_with_flex.button')}
  <ProjectTypeIcon type={ProjectType.FlEx} />
</a>

<style>
  :global(.open-in-flex img) {
    filter: drop-shadow(0 0 0.15rem hsl(var(--b1)));
  }
</style>
