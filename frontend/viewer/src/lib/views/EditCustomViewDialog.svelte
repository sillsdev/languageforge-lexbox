<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import CustomViewForm from './CustomViewForm.svelte';
  import type {CustomView} from './view-data';

  interface Props {
    open: boolean;
    value: CustomView;
    onSave: (result: CustomView) => void | Promise<void>;
  }

  let {open = $bindable(), value, onSave}: Props = $props();

  async function handleSave(result: CustomView) {
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
      submitLabel={$t`Save Changes`}
      onSubmit={handleSave}
      onCancel={handleCancel}
    />
  </Dialog.Content>
</Dialog.Root>
