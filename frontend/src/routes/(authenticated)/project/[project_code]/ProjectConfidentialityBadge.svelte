<script lang="ts">
  import t from '$lib/i18n';
  import { BadgeButton } from '$lib/components/Badges';

  interface Props {
    isConfidential: boolean | undefined;
    canManage: boolean;
    onclick?: () => void;
  }

  const { isConfidential, canManage, onclick }: Props = $props();
</script>

{#if isConfidential !== false}
  <BadgeButton
    variant={isConfidential ? 'badge-warning' : 'badge-neutral'}
    {onclick}
    disabled={!canManage}
    icon={isConfidential === undefined && canManage ? 'i-mdi-pencil-outline' : 'i-mdi-shield-lock-outline'}
    hoverIcon="i-mdi-pencil-outline"
  >
    {#if isConfidential}
      {$t('project.confidential.confidential')}
    {:else if canManage}
      <span class="text-warning">
        {$t('project.confidential.set_confidentiality')}
      </span>
    {:else}
      {$t('project.confidential.confidentiality_unspecified')}
    {/if}
  </BadgeButton>
{/if}
