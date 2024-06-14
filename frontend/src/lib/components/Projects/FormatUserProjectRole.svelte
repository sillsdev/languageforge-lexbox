<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let role: ProjectRole;

  const roles: Record<ProjectRole, string | undefined> = {
    [ProjectRole.Manager]: $t('project_role.manager'),
    [ProjectRole.Editor]: $t('project_role.editor'),
    [ProjectRole.Unknown]: $t('unknown'),
  };

  $: _role = roles[role];
</script>

{_role ?? $t('unknown')}
