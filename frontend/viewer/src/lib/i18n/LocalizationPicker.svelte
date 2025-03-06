<script lang="ts" module>
  const hasSetLang = {value: false};
</script>
<script lang="ts">
import {mdiTranslate} from '@mdi/js';
import {MenuButton, type MenuOption} from 'svelte-ux';
import {locale} from 'svelte-i18n-lingui';
import {onMount} from 'svelte';

const languages: MenuOption[] = [
  {value: 'en', label: 'English'},
  {value: 'fr', label: 'Français'},
  {value: 'es', label: 'Español'},
];
async function setLanguage(lang: string) {
  const wasDefault = lang === 'default';
  if (!lang || wasDefault) lang = localStorage.getItem('locale') ?? 'en';
  let {messages} = await import(`../../locales/${lang}.json?lingui`);
  locale.set(lang, messages);
  //only save when the user changes locale
  if (!wasDefault) localStorage.setItem('locale', lang);
}
onMount(() => {
  if (!hasSetLang.value) {
    void setLanguage($locale);
    hasSetLang.value = true;
  }
});
</script>
<MenuButton
  options={languages}
  value={$locale}
  icon={mdiTranslate}
  on:change={(e) => setLanguage(e.detail.value)}
/>
