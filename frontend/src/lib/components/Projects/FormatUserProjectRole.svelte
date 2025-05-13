<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  interface Props {
    role: ProjectRole;
  }

  let { role }: Props = $props();

  const roles: Record<ProjectRole, string | undefined> = {
    [ProjectRole.Manager]: $t('project_role.manager'),
    [ProjectRole.Editor]: $t('project_role.editor'),
    [ProjectRole.Unknown]: $t('unknown'),
  };

  let _role = $derived(roles[role]);
</script>

{_role ?? $t('unknown')}
