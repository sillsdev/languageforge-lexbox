<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import FieldHelpIcon from '../../../entry-editor/FieldHelpIcon.svelte';
  import {useCurrentView} from '$lib/views/view-service';
  import {pickViewText, type ViewText} from '$lib/views/view-text';

  const {
    name,
    helpId,
  }: {
    name: ViewText;
    helpId?: string | undefined;
  } = $props();

  const view = useCurrentView();
  const label = $derived(pickViewText(name, $view.type));
  const title = $derived(typeof name === 'string' ? undefined
    : $view.type === 'fw-classic'
      ? $t`${name.lite} (FieldWorks Lite)`
      : $t`${name.classic} (FieldWorks)`);

  // kind of crazy, but I don't think Svelte's white-space handling let's us use &nbsp; between the label and help icon
  const { lastWord, otherWords } = $derived.by(() => {
    const words = label.split(' ');
    return {
      lastWord: words.pop(),
      otherWords: words.join(' '),
    };
  });
</script>

<div class="col-span-full me-2 mb-2 @3xl/editor:col-span-1">
  <span class="inline-flex items-center relative">
    <span class="name" {title}>
      {otherWords}
      <span class="whitespace-nowrap">
        {lastWord}
        {#if helpId}
          <FieldHelpIcon {helpId} />
        {/if}
      </span>
    </span>
  </span>
</div>
