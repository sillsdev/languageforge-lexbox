<script lang="ts">
  import Modal from '$lib/components/modals/Modal.svelte';
  import Markdown from 'svelte-exmarkdown';
  import type { Project } from './+page';
  import t from '$lib/i18n';
  import OpenInFlexButton from './OpenInFlexButton.svelte';
  import SendReceiveUrlField from './SendReceiveUrlField.svelte';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';

  export let project: Project;
  let modal: Modal;

  export async function open(): Promise<void> {
    await modal.openModal(true, true);
  }
</script>

<Modal bind:this={modal}>
  <div class="prose open-with-flex-modal max-w-none">
    <h3>{$t('project_page.open_with_flex.button')}</h3>
    <div class="alert alert-info mb-4">
      <span class="i-mdi-info-outline text-xl"></span>
      <Markdown md={$t('project_page.open_with_flex.supported_version')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
    </div>
    <div class="not-prose">
      <Markdown md={$t('project_page.open_with_flex.instructions')} />
    </div>
    <div class="my-4 flex">
      <OpenInFlexButton projectId={project.id} />
    </div>
    <div class="collapse collapse-arrow">
      <input type="checkbox" />
      <h3 class="collapse-title my-0 px-0 pb-0">{$t('project_page.get_project.instructions_header', { type: project.type, mode: 'manual' })}...</h3>
      <div class="collapse-content p-0">
        <div class="divider mt-0" />
        <Markdown md={$t('project_page.get_project.instructions', { code: project.code, name: project.name })} />
        <SendReceiveUrlField projectCode={project.code} />
      </div>
    </div>
  </div>
</Modal>

<style>
  :global(.open-with-flex-modal .collapse-content > *) {
    margin: 0;
  }

  :global(.open-with-flex-modal .alert p) {
    margin: 0;
  }
</style>
