<script lang="ts">
  import t, { type I18nKey } from '$lib/i18n';
  import Select from './Select.svelte';
  import type { UserType } from '$lib/components/Users';

  interface Props {
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
  <!-- TODO: This used to have an on:change attribute to bubble up the submit event from HTML.
       Let's check if the createBubbler() call in Form makes that unnecessary now. -->
  <Select id="type" label={$t('admin_dashboard.user_filter.user_type.label')} bind:value {...rest}>
    {#if undefinedOptionLabel}
      <option value={undefined}>{undefinedOptionLabel}</option>
    {/if}
    {#each Object.entries(options) as [value, label]}
      <option {value}>{$t(label)}</option>
    {/each}
  </Select>
</div>
