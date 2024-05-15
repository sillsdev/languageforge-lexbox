<script lang="ts">
  import { OrgRole, ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let projectRole: ProjectRole | OrgRole;

  const roles: Record<ProjectRole | OrgRole, string | undefined> = {
    [ProjectRole.Manager]: $t('project_role.manager'),
    [ProjectRole.Editor]: $t('project_role.editor'),
    [ProjectRole.Unknown]: $t('unknown'),
    [OrgRole.Admin]: $t('org_role.admin'),
    [OrgRole.User]: $t('org_role.user'),
    // [OrgRole.Unknown]: $t('unknown'), // Already covered by ProjectRole.Unknown
  };

  $: role = roles[projectRole];
</script>

{role ?? $t('unknown')}
