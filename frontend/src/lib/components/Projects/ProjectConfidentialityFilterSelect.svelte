<script lang="ts">
  import { Select } from '$lib/forms';
  import t, { type I18nKey } from '$lib/i18n';
  import type { Confidentiality } from './ProjectFilter.svelte';

  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents -- false positive
  export let value: Confidentiality | undefined;
  const options: Record<Confidentiality, I18nKey> = {
    true: 'project.confidential.confidential',
    false: 'project.confidential.not_confidential',
    unset: 'project.confidential.unspecified',
  };
</script>

<div class="relative">
  <Select label={$t('project.confidential.confidentiality')} bind:value on:change>
    <option value={undefined}>{$t('common.any')}</option>
    {#each Object.entries(options) as [value, label]}
      <option value={value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
