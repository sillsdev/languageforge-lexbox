<script lang="ts">
  import { Select } from '$lib/forms';
  import t, { type I18nKey } from '$lib/i18n';
  import { helpLinks } from '../help';
  import type { Confidentiality } from './ProjectFilter.svelte';

  interface Props {
    // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents -- false positive
    value: Confidentiality | undefined;
  }

  let { value = $bindable() }: Props = $props();
  const options: Record<Confidentiality, I18nKey> = {
    true: 'project.confidential.confidential',
    false: 'project.confidential.not_confidential',
    unset: 'project.confidential.unspecified',
  };
</script>

<div class="relative">
  <!-- TODO: Used to have an on:change attribute below, let's remove it and see if the bubbler function works as it should -->
  <Select label={$t('project.confidential.confidentiality')} helpLink={helpLinks.confidentiality} bind:value>
    <option value={undefined}>{$t('common.any')}</option>
    {#each Object.entries(options) as [value, label]}
      <option {value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
