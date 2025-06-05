<script module lang="ts">
  const roles: Record<ProjectRole, I18nKey | undefined> = {
    [ProjectRole.Manager]: 'project_role.manager',
    [ProjectRole.Editor]: 'project_role.editor',
    [ProjectRole.Observer]: 'project_role.observer',
    [ProjectRole.Unknown]: 'unknown',
  };

  export function formatProjectRole(role: ProjectRole, $t: Translater): string {
    return $t(roles[role] ?? 'unknown');
  }
</script>

<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t, {type I18nKey, type Translater} from '$lib/i18n';

  interface Props {
    role: ProjectRole;
  }

  const { role }: Props = $props();

  let _role = $derived(formatProjectRole(role, $t));
</script>

{_role ?? $t('unknown')}
