<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import CustomViewForm from './CustomViewForm.svelte';
  import type {ICustomView} from '$lib/dotnet-types';

  interface Props {
    open: boolean;
    value: ICustomView;
    onSave: (result: ICustomView) => void | Promise<void>;
  }

  let {open = $bindable(), value, onSave}: Props = $props();

  async function handleSave(result: ICustomView) {
    await onSave(result);
    open = false;
  }

  function handleCancel() {
    open = false;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-5xl">
    <Dialog.Header>
      <Dialog.Title>{$t`Edit Custom View`}</Dialog.Title>
    </Dialog.Header>
    <CustomViewForm
      {value}
      submitLabel={$t`Save View`}
      onSubmit={handleSave}
      onCancel={handleCancel}
    />
  </Dialog.Content>
</Dialog.Root>
