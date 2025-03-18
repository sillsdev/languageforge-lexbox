<script lang="ts">
  import * as Popover from '$lib/components/ui/popover';
  import {Button, type ButtonProps} from '$lib/components/ui/button';
  import {Label} from '$lib/components/ui/label/index.js';
  import type {WithChildren} from 'bits-ui';
  import {Icon} from '$lib/components/ui/icon';
  import {setMode, mode, theme, setTheme} from 'mode-watcher';
  import {cn} from '$lib/utils';
  const { children, button = {} } = $props<WithChildren<{button?: ButtonProps}>>();

  const themes = ['green', 'blue', 'rose', 'orange', 'violet', 'stone'];
</script>
<Popover.Root>
  <Popover.Trigger>
    {#snippet child({props})}
      <Button {...props} {...button}>
        {#if children}
          {@render children()}
        {/if}
      </Button>
    {/snippet}
  </Popover.Trigger>
  <Popover.Content class="w-96">
    <Popover.Close>
      {#snippet child({props})}
        <Button {...props} variant="ghost" size="sm" class="absolute top-2 right-2">
          <Icon icon="i-mdi-close"/>
        </Button>
      {/snippet}
    </Popover.Close>


    <div class="flex flex-1 flex-col space-y-4 md:space-y-6">
      <div class="space-y-1">
        <Label class="text-xs">Color</Label>
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
                  <Icon icon="i-mdi-check" class="text-white"/>
                {/if}
              </span>
              {themeName}
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
            class={cn($mode === 'light' && 'border-primary border-2')}
            icon="i-mdi-white-balance-sunny"
          >
            Light
          </Button>
          <Button
            variant="outline"
            size="sm"
            onclick={() => setMode('dark')}
            class={cn($mode === 'dark' && 'border-primary border-2')}
            icon="i-mdi-weather-night"
          >
            Dark
          </Button>
        </div>
      </div>
    </div>
  </Popover.Content>
</Popover.Root>


