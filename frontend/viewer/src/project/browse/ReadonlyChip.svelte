<script lang="ts">
  import * as Popover from '$lib/components/ui/popover';
  import {Icon} from '$lib/components/ui/icon';
  import {useFeatures} from '$lib/services/feature-service';
  import {useProjectContext} from '$project/project-context.svelte';
  import {t} from 'svelte-i18n-lingui';

  const features = useFeatures();
  const projectContext = useProjectContext();
</script>

{#if projectContext.featuresLoaded && !features.write}
  <Popover.Root>
    <Popover.Trigger
      aria-label={$t`Read-only`}
      class="self-center inline-flex items-center gap-1 h-7 px-2 me-1 rounded-full border text-xs text-muted-foreground hover:text-foreground hover:bg-accent focus-visible:ring-ring/50 focus-visible:ring-[3px] outline-none"
    >
      <Icon icon="i-mdi-lock-outline" class="size-4" />
      <span class="max-sm:hidden max-w-32 truncate">{$t`Read-only`}</span>
    </Popover.Trigger>
    <Popover.Content class="text-sm max-w-72" align="end">
      {#if features.commentWrite}
        {$t`You can read and comment on this project, but not change it.`}
      {:else}
        {$t`You can read this project, but not change it.`}
      {/if}
      {$t`Ask a project manager if you need edit access.`}
    </Popover.Content>
  </Popover.Root>
{/if}
