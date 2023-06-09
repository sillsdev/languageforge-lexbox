<script lang="ts">
  import { beforeNavigate } from '$app/navigation';
  import { dismiss, error } from '$lib/error';
  import { onDestroy } from 'svelte';
  import { t } from 'svelte-intl-precompile';
  import UnexpectedError from './UnexpectedError.svelte';

  let alertContainerElem: HTMLElement;

  beforeNavigate(dismiss);

  onDestroy(
    error.subscribe((e) => {
      alertContainerElem?.classList.toggle('hidden', !e);
    })
  );

  function dismissClick(): void {
    alertContainerElem.classList.add('hidden');
    dismiss();
  }
</script>

<!-- https://daisyui.com/components/alert -->
<aside
  bind:this={alertContainerElem}
  class:hidden={!$error}
  class="block alert alert-error shadow-lg w-auto fixed bottom-4 z-10 left-1/2 -translate-x-1/2 max-w-[95vw] max-h-[95vh] overflow-y-auto"
>
  <UnexpectedError />

  <div class="flex justify-end">
    <button on:click={dismissClick} class="btn btn-ghost self-end">{$t('modal.dismiss')}</button>
  </div>
</aside>
