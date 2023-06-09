<script lang="ts">
  import { RetentionPolicy } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let policy: number | RetentionPolicy;

  const policies: Record<number | RetentionPolicy, string | undefined> = {
    1: $t('retention_policy.language-project'),
    2: $t('retention_policy.test'),
    3: $t('retention_policy.dev'),
    4: $t('retention_policy.training'),
    [RetentionPolicy.Verified]: $t('retention_policy.language-project'),
    [RetentionPolicy.Test]: $t('retention_policy.test'),
    [RetentionPolicy.Dev]: $t('retention_policy.dev'),
    [RetentionPolicy.Training]: $t('retention_policy.training'),
    [RetentionPolicy.Unknown]: $t('unknown'),
  };
</script>

{policies[policy] || $t('unknown')}
