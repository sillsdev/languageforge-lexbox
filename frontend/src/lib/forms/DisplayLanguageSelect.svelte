<script lang="ts">
  import t from '$lib/i18n';
  import { availableLocales } from '$locales';

  import { Select } from '.';

  export let id = 'display-language';
  export let value: string;

  $: currentValueNotSupported = value && !availableLocales.includes(value);
  const localNames: Record<string, string> = {
    en: 'English',
    es: 'Español',
    fr: 'Français',
    kr: '한국어'
  };
</script>

<Select {id} bind:value label={$t('account_settings.language.title')}>
  {#if currentValueNotSupported}
    <!-- Make sure we don't overwrite persisted locales that we don't support yet -->
    <option {value}>{value} ({$t('account_settings.language.not_supported')})</option>
  {/if}
  {#each availableLocales as locale}
    <option value={locale}>{localNames[locale] ?? locale}</option>
  {/each}
</Select>
