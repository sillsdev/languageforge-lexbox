<script lang="ts">
  import {AlertDialog as AlertDialogPrimitive} from 'bits-ui';
  import DialogSharedRoot from '../dialog-shared/dialog-shared-root.svelte';

  let {open = $bindable(false), ...restProps}: AlertDialogPrimitive.RootProps = $props();
</script>

<DialogSharedRoot {open}>
  <AlertDialogPrimitive.Root bind:open {...restProps} />
</DialogSharedRoot>
