<script lang="ts" module>
  import type {CustomViewFormValue} from './CustomViewForm.svelte';

  export type CreateCustomViewResult = CustomViewFormValue;
</script>

<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import CustomViewForm from './CustomViewForm.svelte';
  type FormValue = import('./CustomViewForm.svelte').CustomViewFormValue;

  interface Props {
    open: boolean;
    defaultBaseViewId?: 'fwlite' | 'fieldworks';
    onCreate: (result: FormValue) => void | Promise<void>;
  }

  let {open = $bindable(), defaultBaseViewId = 'fwlite', onCreate}: Props = $props();
  let resetToken = $state(0);
  let wasOpen = false;

  $effect(() => {
    if (open && !wasOpen) {
      resetToken += 1;
    }
    wasOpen = open;
  });

  async function handleCreate(result: FormValue) {
    await onCreate(result);
    open = false;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-lg">
    <Dialog.Header>
      <Dialog.Title>{$t`Create Custom View`}</Dialog.Title>
    </Dialog.Header>

    <CustomViewForm
      initialValue={{baseViewId: defaultBaseViewId, label: '', fieldIds: []}}
      {resetToken}
      submitLabel={$t`Create View`}
      onSubmit={handleCreate}
      onCancel={() => open = false}
    />
  </Dialog.Content>
</Dialog.Root>
