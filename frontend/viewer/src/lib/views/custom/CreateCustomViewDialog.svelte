<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import CustomViewForm from './CustomViewForm.svelte';
  import type {ICustomView} from '$lib/dotnet-types';

  interface Props {
    open: boolean;
    onCreate: (result: ICustomView) => void | Promise<void>;
  }

  let {open = $bindable(), onCreate}: Props = $props();

  async function handleCreate(result: ICustomView) {
    await onCreate(result);
    open = false;
  }

  function handleCancel() {
    open = false;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-5xl">
    <Dialog.Header>
      <Dialog.Title>{$t`Create Custom View`}</Dialog.Title>
    </Dialog.Header>

    <CustomViewForm
      submitLabel={$t`Create View`}
      onSubmit={handleCreate}
      onCancel={handleCancel}
    />
  </Dialog.Content>
</Dialog.Root>
