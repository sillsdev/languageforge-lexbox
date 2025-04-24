<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import FieldHelpIcon from '../../../entry-editor/FieldHelpIcon.svelte';
  import {useCurrentView} from '$lib/views/view-service';

  const {
    liteName,
    classicName,
    helpId,
  }: {
    liteName: string;
    classicName: string;
    helpId?: string | undefined;
  } = $props();

  const view = useCurrentView();

  const { label, title } = $derived(
    $view.type === 'fw-classic'
      ? {
          label: classicName,
          title: $t`${liteName} (FieldWorks Lite)`,
        }
      : {
          label: liteName,
          title: $t`${classicName} (FieldWorks)`,
        },
  );

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
