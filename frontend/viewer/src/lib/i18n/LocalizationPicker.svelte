<script lang="ts">
import {mdiTranslate} from '@mdi/js';
import {MenuButton, type MenuOption} from 'svelte-ux';
import {locale} from 'svelte-i18n-lingui';

const languages: MenuOption[] = [
  {value: 'en', label: 'English'},
  {value: 'fr', label: 'Français'},
  {value: 'es', label: 'Español'},
];
async function setLanguage(lang: string) {
  let {messages} = await import(`../../locales/${lang}.json?lingui`);
  locale.set(lang, messages);
}
</script>
<MenuButton
  options={languages}
  value="en"
  icon={mdiTranslate}
  on:change={(e) => setLanguage(e.detail.value)}
/>
