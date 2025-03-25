<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Label} from '$lib/components/ui/label/index.js';
  import * as Popover from '$lib/components/ui/popover';
  import {cn} from '$lib/utils';
  import {mode, resetMode, setMode, setTheme, theme, userPrefersMode} from 'mode-watcher';
  import {t} from 'svelte-i18n-lingui';

  const themes = ['green', 'blue', 'rose', 'orange', 'violet', 'stone'];
</script>
<Popover.Root>
  <Popover.Trigger>
    {#snippet child({props})}
      <Button variant="ghost" size="icon" {...props}>
        <Icon icon="i-mdi-white-balance-sunny"
          class="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0 text-primary"
        />
        <Icon icon="i-mdi-weather-night"
          class="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100 text-primary"
        />
        <span class="sr-only">{$t`Toggle theme`}</span>
      </Button>
    {/snippet}
  </Popover.Trigger>
  <Popover.Content class="w-96">
    <div class="flex flex-1 flex-col space-y-4 md:space-y-6">
      <div class="space-y-1">
        <div class="flex items-baseline justify-between">
          <Label class="text-xs">{$t`Color`}</Label>
          <Popover.Close>
            {#snippet child({props})}
              <Button {...props} variant="ghost" size="icon">
                <Icon icon="i-mdi-close"/>
              </Button>
            {/snippet}
          </Popover.Close>
        </div>
        <div class="grid grid-cols-3 gap-2">
          {#each themes as themeName (themeName)}
            {@const isActive = $theme === themeName}
            <Button
              variant="outline"
              size="sm"
              onclick={() => {
                setTheme(themeName);
              }}
              class={cn('justify-start', isActive && 'border-primary border-2')}
            >
              <span
                data-theme={themeName}
                class={cn('flex size-5 shrink-0 -translate-x-1 items-center justify-center rounded-full bg-primary',
                $mode === 'dark' && 'dark',
                $mode === 'light' && 'light')}
              >
                {#if isActive}
                  <Icon icon="i-mdi-check" class="text-white size-4"/>
                {/if}
              </span>
              <span class="capitalize">
                {themeName}
              </span>
            </Button>
          {/each}
        </div>
      </div>
      <div class="space-y-1.5">
        <Label class="text-xs">Mode</Label>
        <div class="grid grid-cols-3 gap-2">
          <Button
            variant="outline"
            size="sm"
            onclick={() => setMode('light')}
            class={cn($userPrefersMode === 'light' && 'border-primary border-2')}
            icon="i-mdi-white-balance-sunny"
          >
            Light
          </Button>
          <Button
            variant="outline"
            size="sm"
            onclick={() => setMode('dark')}
            class={cn($userPrefersMode === 'dark' && 'border-primary border-2')}
            icon="i-mdi-weather-night"
          >
            Dark
          </Button>
          <Button
            variant="outline"
            size="sm"
            onclick={() => resetMode()}
            class={cn($userPrefersMode === 'system' && 'border-primary border-2')}
            icon="i-mdi-laptop"
          >
            System
          </Button>
        </div>
      </div>
    </div>
  </Popover.Content>
</Popover.Root>


