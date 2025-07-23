<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import RadioGroup from '$lib/components/ui/radio-group/radio-group.svelte';
  import RadioGroupItem from '$lib/components/ui/radio-group/radio-group-item.svelte';

  let open = $state(false);
  let loading = $state(false);
  let error: string|undefined = $state();
  const validRoles: {role: UserProjectRole, label: string}[] = [
    { role: UserProjectRole.Manager, label: $t`Manager` },
    { role: UserProjectRole.Editor, label: $t`Editor` },
    { role: UserProjectRole.Observer, label: $t`Observer` },
  ];
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
      <RadioGroup bind:value={userRole}>
        {#each validRoles as role (role.role)}
          <RadioGroupItem label={role.label} value={role.role} />
        {/each}
      </RadioGroup>
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
