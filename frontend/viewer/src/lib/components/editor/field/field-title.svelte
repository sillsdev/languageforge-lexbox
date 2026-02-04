<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import FieldHelpIcon from '../../../entry-editor/FieldHelpIcon.svelte';
  import {useCurrentView} from '$lib/views/view-service';
  import {pickViewText, type ViewText} from '$lib/views/view-text';
  import {useFieldTitle} from './field-root.svelte';

  const stateProps = useFieldTitle();

  const {
    name,
    helpId,
  }: {
    name: ViewText;
    helpId?: string | undefined;
  } = $props();

  const view = useCurrentView();
  const label = $derived(pickViewText(name, $view.type));
  $effect(() => {
    stateProps.label = label;
  });
  const title = $derived(
    typeof name === 'string'
      ? undefined
      : $view.type === 'fw-classic'
        ? $t`${name.lite} (FieldWorks Lite)`
        : $t`${name.classic} (FieldWorks)`,
  );
</script>

<span class="col-span-full me-2 mb-2 @3xl/editor:col-span-1 max-w-max" {title}>
  <span id={stateProps.labelId}>{label}</span><span class="whitespace-nowrap"
    >&#8288;<!-- non-breaking space that glues the last word of the label to the help icon -->
    {#if helpId}
      <FieldHelpIcon {helpId} />
    {/if}
  </span>
</span>
