<script lang="ts">
  import t, { type I18nKey } from '$lib/i18n';
  import Select, { type SelectProps } from './Select.svelte';
  import type { UserType } from '$lib/components/Users';

  interface Props extends SelectProps {
    value: UserType;
    undefinedOptionLabel?: string | undefined;
  }

  let { value = $bindable(), undefinedOptionLabel = undefined, ...rest }: Props = $props();

  const options: Record<Exclude<UserType, undefined>, I18nKey> = {
    admin: 'admin_dashboard.user_filter.user_type.admin',
    nonAdmin: 'admin_dashboard.user_filter.user_type.nonAdmin',
    guest: 'admin_dashboard.user_filter.user_type.guest',
  };
</script>

<div class="relative">
  <!-- Note: important that ...rest be at the start of the attributes list so that `label={$t(...)}` will not be overridden by the `label` prop in ...rest -->
  <Select {...rest} id="type" label={$t('admin_dashboard.user_filter.user_type.label')} bind:value>
    {#if undefinedOptionLabel}
      <option value={undefined}>{undefinedOptionLabel}</option>
    {/if}
    {#each Object.entries(options) as [value, label]}
      <option {value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
