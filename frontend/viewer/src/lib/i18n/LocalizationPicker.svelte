<script lang="ts">
  import {locale} from 'svelte-i18n-lingui';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {setLanguage} from '$lib/i18n';

  const languages: Record<string, string> = {
    'en': 'English',
    'fr': 'Français',
    'es': 'Español',
    'id': 'Bahasa Indonesia',
    'ko': '한국어'
  };
  const currentLanguage = $derived(languages[$locale] ?? 'Unknown: ' + $locale);
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
