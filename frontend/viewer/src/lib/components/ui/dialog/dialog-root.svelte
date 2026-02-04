<script lang="ts">
  import {Dialog as DialogPrimitive} from 'bits-ui';
  import DialogSharedRoot from '../dialog-shared/dialog-shared-root.svelte';

  let {open = $bindable(false), ...restProps}: DialogPrimitive.RootProps = $props();
</script>

<DialogSharedRoot {open}>
  <DialogPrimitive.Root bind:open {...restProps} />
</DialogSharedRoot>
