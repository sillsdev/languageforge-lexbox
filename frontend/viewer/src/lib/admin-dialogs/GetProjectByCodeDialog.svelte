<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  let open = $state(false);
  let loading = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'get-project-by-code-dialog'});

  let errors: string[] = $state([]);
  function notAuthorized(projectCode: string) {
    errors.push(`Not authorized for project ${projectCode}`);
  }
  function downloadProject(e: Event, projectCode: string) {
    e.preventDefault();
    e.stopPropagation();
    loading = true;
    console.debug('Downloading project', projectCode);
    // TODO: Download the project here
    var authorized = true; // This will, of course, come from the API once I implement it
    if (!authorized)
    {
      notAuthorized(projectCode);
    }
    loading = false;
    open = false;
  }

  export function openDialog()
  {
    open = true;
  }

  let inputValue = $state('');
</script>

{#if open}
<Dialog.Root bind:open={open}>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Download project by project code`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if errors.length}
      <div class="text-end space-y-2">
        {#each errors as error}
          <p class="text-destructive">{error}</p>
        {/each}
      </div>
    {/if}
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={e => downloadProject(e, inputValue)} disabled={loading} {loading}>
        {$t`Download ${inputValue}`}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
{/if}
