<script lang="ts">
  import {locale} from 'svelte-i18n-lingui';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {setLanguage} from '$lib/i18n';
  import {SidebarMenuButton} from '$lib/components/ui/sidebar';

  type Props = {
    inSidebar?: boolean;
  };

  let {
    inSidebar
  }: Props = $props();

  const languages: Record<string, string> = {
    'en': 'English',
    'es': 'Español',
    'fr': 'Français',
    'id': 'Bahasa Indonesia',
    'ko': '한국어',
    'ms': 'Bahasa Malaysia, بهاس مليسيا',
    'sw': 'Kiswahili',
    'vi': 'Tiếng Việt',
  };
  const currentLanguage = $derived(languages[$locale] ?? 'Unknown: ' + $locale);
</script>

<DropdownMenu.Root>
  <DropdownMenu.Trigger>
    {#snippet child({props})}
      {#if inSidebar}
        <SidebarMenuButton {...props}>
          <Icon icon="i-mdi-translate" />
          <span>{currentLanguage}</span>
          <span class="grow"></span>
          <Icon icon="i-mdi-menu-down"/>
        </SidebarMenuButton>
      {:else}
        <Button {...props} icon="i-mdi-translate" size={IsMobile.value ? 'icon' : undefined} variant="ghost">
          {#if !IsMobile.value}
            {currentLanguage}
            <Icon icon="i-mdi-menu-down"/>
          {/if}
        </Button>
      {/if}
    {/snippet}
  </DropdownMenu.Trigger>
  <DropdownMenu.Content align="end">
    <DropdownMenu.RadioGroup bind:value={() => $locale, l => setLanguage(l)}>
      {#each Object.entries(languages) as [lang, label] (lang)}
        <DropdownMenu.RadioItem class="cursor-pointer" value={lang}>{label}</DropdownMenu.RadioItem>
      {/each}
    </DropdownMenu.RadioGroup>
  </DropdownMenu.Content>
</DropdownMenu.Root>
