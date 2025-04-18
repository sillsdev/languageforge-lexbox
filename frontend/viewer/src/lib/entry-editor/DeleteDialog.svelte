<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {mdiTrashCanOutline} from '@mdi/js';
  import {t} from 'svelte-i18n-lingui';
  let subject: string = $state('');
  let open = $state(false);
  let requester: {
    resolve: (result: boolean) => void
  } | undefined = undefined;
  $effect(() => {
    if (!open && requester) resolve(false);
  });

  function confirm() {
    resolve(true);
  }

  function cancel() {
    resolve(false);
  }

  function resolve(shouldDelete: boolean) {
    requester?.resolve(shouldDelete);
    requester = undefined;
    open = false;
  }

  export function prompt(promptSubject: string): Promise<boolean> {
    if (requester) throw new Error('already prompting for a delete');
    return new Promise((resolve) => {
      requester = { resolve };
      subject = promptSubject;
      open = true;
    });
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Delete ${subject}`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div class="m-6 mt-3">
      <p>{$t`Are you sure you want to delete ${subject}?`}</p>
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => cancel()} variant="secondary">{$t`Don't delete`}</Button>
      <Button icon="i-mdi-trash-can-outline" variant="destructive" onclick={_ => confirm()}>{$t`Delete ${subject}`}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
