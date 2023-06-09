<script lang="ts">
  import t from '$lib/i18n';

  export let projectRole: number;

  const roles: Record<number, string | undefined> = {
    2: $t('project_role.manager'),
    3: $t('project_role.editor'),
  };

  $: role = roles[projectRole];
</script>

{role ?? $t('unknown')}
