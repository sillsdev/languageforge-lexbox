<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
  import {useProjectContext} from '$project/project-context.svelte';
  import {t} from 'svelte-i18n-lingui';

  let {onClose}: {onClose?: () => void} = $props();
  const projectContext = useProjectContext();
  const dialogsService = useDialogsService();
  const canManageCustomViews = $derived(projectContext.projectData?.role === UserProjectRole.Manager);
</script>

{#if canManageCustomViews}
  <Button
    variant="outline"
    size="sm"
    class="w-full"
    icon="i-mdi-cog-outline"
    onclick={() => { onClose?.(); dialogsService.openManageCustomViews(); }}
  >
    {$t`Manage custom views`}
  </Button>
{/if}
