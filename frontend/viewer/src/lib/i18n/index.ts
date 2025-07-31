import {locale} from 'svelte-i18n-lingui';

const hasSetLang = {value: false};

export async function setLanguage(lang: string): Promise<void> {
  const wasDefault = lang === 'default';
  if (!lang || wasDefault) lang = localStorage.getItem('locale') ?? 'en';
  // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
  const {messages} = await import(`../../locales/${lang}.po?lingui`);
  locale.set(lang, messages);
  //only save when the user changes locale
  if (!wasDefault) localStorage.setItem('locale', lang);
  hasSetLang.value = true;
}
