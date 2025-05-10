<script lang="ts">
  import t from '$lib/i18n';
  import { Icon } from '$lib/icons';

  interface Props {
    locked: boolean;
  }

  const { locked }: Props = $props();
</script>

{#if locked}
  <span class="alert alert-warning my-4">
    <Icon icon="i-mdi-lock" />
    {$t('admin_dashboard.user_is_locked')}
  </span>
{/if}
