<script lang="ts">
  import t from '$lib/i18n';

  // eslint-disable-next-line no-restricted-imports
  import { t as otherT } from 'svelte-intl-precompile';
</script>

<div class="grid gap-2 grid-cols-3">
  <div class="card bg-base-200 shadow-lg col-span-2">
    <div class="card-body">
      <h2 class="card-title">Translations</h2>
      <div class="current">Current: {$t('login.title')}</div>
      <div>English: {$otherT('login.title', { locale: 'en' })} (always works, because it's the fallback)</div>
      <div>French: {$otherT('login.title', { locale: 'fr' })} (always works, because the server has all locales loaded)</div>
      <div>Spanish: {$otherT('login.title', { locale: 'es' })} (always works, because the server has all locales loaded)</div>
    </div>
  </div>
</div>
