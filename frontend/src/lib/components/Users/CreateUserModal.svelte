<script lang="ts">
  import { Modal } from '$lib/components/modals';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';

  let createUserModal: Modal;

  export async function open(): Promise<void> {
    await createUserModal.openModal(true, true);
  }
</script>

<Modal bind:this={createUserModal} bottom>
  <CreateUser autoLogin={false} />
</Modal>
