<script lang="ts">
  import { Select } from '$lib/forms';
  import { type SelectProps } from '$lib/forms/Select.svelte';
  import t, { type I18nKey } from '$lib/i18n';
  import { helpLinks } from '../help';
  import type { Confidentiality } from './ProjectFilter.svelte';

  interface Props extends Omit<SelectProps, 'label'> {
    // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents -- false positive
    value: Confidentiality | undefined;
  }

  let { value = $bindable(), ...rest }: Props = $props();
  const options: Record<Confidentiality, I18nKey> = {
    true: 'project.confidential.confidential',
    false: 'project.confidential.not_confidential',
    unset: 'project.confidential.unspecified',
  };
</script>

<div class="relative">
  <Select {...rest} label={$t('project.confidential.confidentiality')} helpLink={helpLinks.confidentiality} bind:value>
    <option value={undefined}>{$t('common.any')}</option>
    {#each Object.entries(options) as [value, label] (value)}
      <option {value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
