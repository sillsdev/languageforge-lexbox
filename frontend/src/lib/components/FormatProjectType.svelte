<script lang="ts">
  import { ProjectType } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let type: number | ProjectType;

  const types: Record<number | ProjectType, string | undefined> = {
    1: $t('project_type.flex'),
    2: $t('project_type.weSay'),
    3: $t('project_type.oneStoryEditor'),
    4: $t('project_type.ourWord'),
    [ProjectType.FlEx]: $t('project_type.flex'),
    [ProjectType.WeSay]: $t('project_type.weSay'),
    [ProjectType.OneStoryEditor]: $t('project_type.oneStoryEditor'),
    [ProjectType.OurWord]: $t('project_type.ourWord'),
    [ProjectType.Unknown]: $t('project_type.other'),
  };
</script>

{types[type] || $t('unknown')}
