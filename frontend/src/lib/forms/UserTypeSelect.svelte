<script lang="ts">
  import t, { type I18nKey } from '$lib/i18n';
  import Select from './Select.svelte';
  import type { UserType } from '$lib/components/Users';

  export let value: UserType;
  export let undefinedOptionLabel: string | undefined = undefined;

  const options: Record<Exclude<UserType, undefined>, I18nKey> = {
    admin: 'admin_dashboard.user_filter.user_type.admin',
    nonAdmin: 'admin_dashboard.user_filter.user_type.nonAdmin',
    guest: 'admin_dashboard.user_filter.user_type.guest'
  };
</script>

<div class="relative">
  <Select id="type" label={$t('admin_dashboard.user_filter.user_type.label')} bind:value on:change>
    {#if undefinedOptionLabel}
      <option value={undefined}>{undefinedOptionLabel}</option>
    {/if}
    {#each Object.entries(options) as [value, label]}
      <option value={value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
