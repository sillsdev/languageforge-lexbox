<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  interface Props {
    role: OrgRole;
  }

  const { role }: Props = $props();

  const roles: Record<OrgRole, string | undefined> = {
    [OrgRole.Admin]: $t('org_role.admin'),
    [OrgRole.User]: $t('org_role.user'),
    [OrgRole.Unknown]: $t('unknown'),
  };

  let _role = $derived(roles[role]);
</script>

{_role ?? $t('unknown')}
