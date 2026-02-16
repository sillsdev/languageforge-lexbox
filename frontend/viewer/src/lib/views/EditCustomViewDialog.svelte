<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import CustomViewForm, {type CustomViewFormInitialValue, type CustomViewFormValue} from './CustomViewForm.svelte';

  interface Props {
    open: boolean;
    initialValue?: CustomViewFormInitialValue;
    onSave: (result: CustomViewFormValue) => void | Promise<void>;
  }

  let {open = $bindable(), initialValue, onSave}: Props = $props();
  let resetToken = $state(0);
  let wasOpen = false;

  $effect(() => {
    if (open && !wasOpen) {
      resetToken += 1;
    }
    wasOpen = open;
  });

  async function handleSave(result: CustomViewFormValue) {
    await onSave(result);
    open = false;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-lg">
    <Dialog.Header>
      <Dialog.Title>{$t`Edit Custom View`}</Dialog.Title>
    </Dialog.Header>

    {#if initialValue}
      <CustomViewForm
        {initialValue}
        {resetToken}
        submitLabel={$t`Save Changes`}
        onSubmit={handleSave}
        onCancel={() => open = false}
      />
    {/if}
  </Dialog.Content>
</Dialog.Root>
