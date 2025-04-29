<script lang="ts">
  import t from '$lib/i18n';
  import Checkbox from '../../forms/Checkbox.svelte';
  import { slide } from 'svelte/transition';
  import { Icon } from '$lib/icons';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';

  interface Props {
    value: boolean;
  }

  let { value = $bindable() }: Props = $props();
</script>

<Checkbox
  id="confidentiality"
  label={$t('project.confidential.label')}
  description={$t('project.confidential.description')}
  variant="checkbox-warning"
  labelColor="text-warning"
  bind:value
/>

{#if value}
  <div class="alert alert-warning mt-4" transition:slide>
    <Icon icon="i-mdi-alert-outline" />
    <NewTabLinkMarkdown md={$t('project.confidential.suggestions')} />
  </div>
{/if}
