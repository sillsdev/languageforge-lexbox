<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import Select from '$lib/components/field-editors/select.svelte';

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
    try {
      error = await onDownloadProject(projectCode, userRole);
    } catch (e) {
      // Set error message to prevent auto-closing dialog
      error = $t`Unknown error downloading ${projectCode}`;
      throw e; // Allow notification to catch it and display the exception
    } finally {
      loading = false;
      open = !!error;
    }
  }

  let projectCode = $state('');
  let codeValidationError = $derived(validateCode(projectCode));
  const roleSelections: {role: UserProjectRole, label: string}[] = [
    { role: UserProjectRole.Manager, label: $t`Manager` },
    { role: UserProjectRole.Editor, label: $t`Editor` },
    { role: UserProjectRole.Observer, label: $t`Observer` },
  ];
  const observerSelection = roleSelections[2];
  let selectedRole: {role: UserProjectRole, label: string} = $state(observerSelection);

  export function openDialog() {
    error = undefined;
    projectCode = '';
    selectedRole = observerSelection;
    loading = false;
    open = true;
  }
</script>


<Dialog.Root bind:open={open}>
  <Dialog.DialogContent hideClose={loading} interactOutsideBehavior="ignore" escapeKeydownBehavior="ignore">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Download project by project code`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div class="flex flex-col gap-2">
      <Label class="cursor-pointer flex items-center gap-2">
        Code:
        <Input bind:value={projectCode} />
      </Label>
      <Label class="cursor-pointer flex items-center gap-2">
        Role:
        <Select
          options={roleSelections}
          bind:value={selectedRole}
          idSelector="role"
          labelSelector="label"
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
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary" disabled={loading}>{$t`Cancel`}</Button>
      <Button onclick={e => downloadProject(e, projectCode, selectedRole.role)} disabled={loading || !!codeValidationError} {loading}>
        {$t`Download ${projectCode}`}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
