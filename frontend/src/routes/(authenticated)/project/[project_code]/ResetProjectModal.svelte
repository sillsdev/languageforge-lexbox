<script lang="ts">
  import Modal from '$lib/components/modals/Modal.svelte';
  import Form from '$lib/forms/Form.svelte';
  import t from '$lib/i18n';
  import Checkbox from '$lib/forms/Checkbox.svelte';
  import Button from '$lib/forms/Button.svelte';

  let modal: Modal;

  export async function open(): Promise<void> {
    console.log('Opening modal', modal);
    await modal.openModal();
  }

  export function close(): void {
    modal.close();
  }

  export function resetProject(): void {
    alert('Would reset project');
  }

  let confirmCheck = false;
</script>

<Modal bind:this={modal} on:close={() => close()} bottom>
  <Form id="modalForm">
    <p>{$t('project_page.reset_project_model.title', {name: 'project?.name'})}</p>
    <Checkbox bind:value={confirmCheck} label={$t('project_page.reset_project_model.confirm_downloaded')} />
  </Form>
  <svelte:fragment slot="actions" let:submitting>
    <Button type="submit" on:click style="btn-primary" loading={submitting} disabled={!confirmCheck}>
      <span>{$t('project_page.reset_project')}</span>
    </Button>
  </svelte:fragment>
</Modal>
