<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
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

<AlertDialog.Root bind:open>
  <AlertDialog.Content>
    <AlertDialog.Header>
      <AlertDialog.Title>{$t`Delete ${subject}`}</AlertDialog.Title>
    </AlertDialog.Header>
    <AlertDialog.Description>
      {$t`Are you sure you want to delete ${subject}?`}
    </AlertDialog.Description>
    <AlertDialog.Footer>
      <Button onclick={() => cancel()} variant="secondary">{$t`Don't delete`}</Button>
      <Button icon="i-mdi-trash-can-outline" variant="destructive" onclick={_ => confirm()}>{$t`Delete ${subject}`}</Button>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
