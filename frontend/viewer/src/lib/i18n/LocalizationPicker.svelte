<script lang="ts" module>
  const hasSetLang = {value: false};
</script>
<script lang="ts">
import {locale} from 'svelte-i18n-lingui';
import {onMount} from 'svelte';
import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
import {Button} from '$lib/components/ui/button';
import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

const languages: Record<string, string> = {
  'en': 'English',
  'fr': 'Français',
  'es': 'Español'
};
const currentLanguage = $derived(languages[$locale] ?? 'Unknown: ' + $locale);
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
<DropdownMenu.Root>
  <DropdownMenu.Trigger>
    {#snippet child({props})}
      <Button {...props} icon="i-mdi-translate" size={IsMobile.value ? 'icon' : undefined} variant="ghost">
        {#if !IsMobile.value}
          {currentLanguage}
          <Icon icon="i-mdi-menu-down"/>
        {/if}
      </Button>
    {/snippet}
  </DropdownMenu.Trigger>
  <DropdownMenu.Content>
    <DropdownMenu.RadioGroup bind:value={() => $locale, l => setLanguage(l)}>
      {#each Object.entries(languages) as [lang, label]}
        <DropdownMenu.RadioItem class="cursor-pointer" value={lang}>{label}</DropdownMenu.RadioItem>
      {/each}
    </DropdownMenu.RadioGroup>
  </DropdownMenu.Content>
</DropdownMenu.Root>
