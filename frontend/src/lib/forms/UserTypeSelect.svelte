<script lang="ts">
  import t, { type I18nKey } from '$lib/i18n';
  import Select, { type SelectProps } from './Select.svelte';
  import type { UserType } from '$lib/components/Users';

  interface Props extends Omit<SelectProps, 'label'> {
    value: UserType;
    undefinedOptionLabel?: string;
  }

  let { value = $bindable(), undefinedOptionLabel, ...rest }: Props = $props();

  const options: Record<Exclude<UserType, undefined>, I18nKey> = {
    admin: 'admin_dashboard.user_filter.user_type.admin',
    nonAdmin: 'admin_dashboard.user_filter.user_type.nonAdmin',
    guest: 'admin_dashboard.user_filter.user_type.guest',
  };
</script>

<div class="relative">
  <Select {...rest} id="type" label={$t('admin_dashboard.user_filter.user_type.label')} bind:value>
    {#if undefinedOptionLabel}
      <option value={undefined}>{undefinedOptionLabel}</option>
    {/if}
    {#each Object.entries(options) as [value, label] (value)}
      <option {value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
