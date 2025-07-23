<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import Select from '$lib/components/field-editors/select.svelte';
  import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';

  let open = $state(false);
  let loading = $state(false);
  let error: string|undefined = $state();
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'get-project-by-code-dialog'});

  let { onDownloadProject, validateCode }: {
    onDownloadProject: (code: string, userRole: UserProjectRole) => Promise<string | undefined>,
    validateCode: (code: string) => string | undefined,
  } = $props();

  async function downloadProject(e: Event, projectCode: string, userRole: UserProjectRole) {
    e.preventDefault();
    e.stopPropagation();
    loading = true;
    error = await onDownloadProject(projectCode, userRole);
    loading = false;
    open = !!error;
  }

  let projectCode = $state('');
  let codeValidationError = $derived(validateCode(projectCode));
  let userRole: UserProjectRole = $state(UserProjectRole.Observer);
  const validRoles = Object.keys(UserProjectRole).filter((role) => (role as keyof typeof UserProjectRole) !== 'Unknown');

  export function openDialog()
  {
    error = undefined;
    projectCode = '';
    userRole = UserProjectRole.Observer;
    loading = false;
    open = true;
  }
</script>

{#if open}
<Dialog.Root bind:open={open}>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Download project by project code`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <Label class="cursor-pointer flex items-center gap-2">
      Code:
      <Input bind:value={projectCode} />
    </Label>
    <Label class="cursor-pointer flex items-center gap-2">
      Role:
      <Select
          bind:value={userRole}
          options={validRoles}
          labelSelector={(role: UserProjectRole) => $t(role)}
          idSelector={(role: UserProjectRole) => role}
          />
    </Label>
    <div class="text-end space-y-2">
      {#if error}
        <p class="text-destructive">{error}</p>
      {/if}
      {#if codeValidationError}
        <p class="text-destructive">{codeValidationError}</p>
      {/if}
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={e => downloadProject(e, projectCode, userRole)} disabled={loading || !!codeValidationError} {loading}>
        {$t`Download ${projectCode}`}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
{/if}
