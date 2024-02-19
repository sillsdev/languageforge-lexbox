<script context="module" lang="ts">
  import { ProjectType } from '$lib/gql/types';
  import t, { type I18nKey } from '$lib/i18n';

  const types: Record<ProjectType, I18nKey | undefined> = {
    [ProjectType.FlEx]: 'project_type.flex',
    [ProjectType.WeSay]: 'project_type.weSay',
    [ProjectType.OneStoryEditor]: 'project_type.oneStoryEditor',
    [ProjectType.OurWord]: 'project_type.ourWord',
    [ProjectType.Unknown]: 'project_type.other',
    [ProjectType.AdaptIt]: 'project_type.adaptIt',
  };

  export function getProjectTypeI18nKey(type: ProjectType): I18nKey {
    return types[type] ?? 'unknown';
  }
</script>

<script lang="ts">
  export let type: ProjectType;
</script>

{$t(getProjectTypeI18nKey(type))}
