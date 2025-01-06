<script lang="ts">
  import {Button, Dialog} from 'svelte-ux';
  import {mdiTrashCanOutline} from '@mdi/js';
  let subject: string;
  let open = false;
  let loading = false;
  let requester: {
    resolve: (result: boolean) => void
  } | undefined = undefined;

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
<Dialog {open} on:close={cancel} {loading} persistent={loading} style="height: auto">
  <div slot="title">Delete {subject}</div>
  <div class="m-6">
    <p>Are you sure you want to delete {subject}?</p>
  </div>
  <div slot="actions">
    <Button on:click={() => cancel()}>Don't delete</Button>
    <Button variant="fill-light" color="danger" icon={mdiTrashCanOutline} on:click={_ => confirm()}>Delete {subject}</Button>
  </div>
</Dialog>
