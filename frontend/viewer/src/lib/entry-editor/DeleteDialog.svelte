<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {mdiTrashCanOutline} from '@mdi/js';
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
  <Dialog.DialogContent class="sm:max-w-[425px]">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Delete {subject}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div class="m-6 mt-3">
      <p>Are you sure you want to delete {subject}?</p>
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => cancel()} variant="secondary">Don't delete</Button>
      <Button icon="i-mdi-trash-can-outline" variant="destructive" onclick={_ => confirm()}>Delete {subject}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
