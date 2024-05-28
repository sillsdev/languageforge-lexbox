<script lang="ts">
  import type { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let value: OrgRole;
  $: lower = value.toLowerCase() as Lowercase<OrgRole>;
  export let verbose = false;
</script>

{$t(verbose ? `org_role.${lower}_description` : `org_role.${lower}`)}
