<script lang="ts">
  import { Modal } from '$lib/components/modals';
  // import t from '$lib/i18n'; // TODO
  import { helpLinks } from '$lib/components/help';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';

  let createUserModal: Modal;

  export async function open(): Promise<void> {
    await createUserModal.openModal(true, true);
  }
</script>

<Modal bind:this={createUserModal} bottom>
  <!-- TODO: Move to en.json and use $t -->
  <p>
    Did you know you can also <a class="link" rel="external" href={helpLinks.addProjectMember}>create users on a project page</a>? That also allows you to make those users members of the project in one step;
    users created here will not be members of any project and will need a second step to add them to a project.
  </p>
  <p>
    If you want to create multiple users at once, the best way is probably to use the <a class="link" rel="external" href={helpLinks.bulkAddCreate}>Bulk Add/Create Project Members</a> tool.
  </p>
  <CreateUser autoLogin={false} onSubmit={() => createUserModal.submitModal()} />
</Modal>
