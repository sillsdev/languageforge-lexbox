<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let projectRole: ProjectRole;

  const roles: Record<ProjectRole, string | undefined> = {
    [ProjectRole.Manager]: $t('project_role.manager'),
    [ProjectRole.Editor]: $t('project_role.editor'),
    [ProjectRole.Unknown]: $t('unknown')
  };

  $: role = roles[projectRole];
</script>

{role ?? $t('unknown')}
