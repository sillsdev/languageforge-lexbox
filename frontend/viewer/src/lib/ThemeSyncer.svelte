<script lang="ts" module>
  let syncingThemes = false;
</script>

<script lang="ts">
  import { mode, setMode, theme, userPrefersMode as userPrefersModeStore } from 'mode-watcher';
  import { getSettings } from 'svelte-ux';
  import { watch } from 'runed';

  const { currentTheme: currentThemeStore } = getSettings();

  /**
   * Summary:
   * This whole mess is a essentially a hack that we can throw away with svelte-ux to ensure that the themes and modes of our different UIs stay in sync
   *
   * svelte-ux uses the data-theme attribute for both mode and theme,
   * so we change shadcn to put it's data-theme attribute only on it's own root and make svelte-ux happy by putting data-theme=dark|light on the html element.
   */

  watch(
    [() => $mode, () => $theme, () => $userPrefersModeStore, () => $currentThemeStore],
    ([mode, theme, userPrefersMode, currentTheme], [prevMode, prevTheme, prevUserPrefersMode, prevCurrentTheme]) => {

      if (syncingThemes) return;
      syncingThemes = true;
      // debounce call to setMode below / prevent duplicate executions due to multiple instances of this component
      setTimeout(() => syncingThemes = false, 2);

      const shadcnRoot = document.querySelector('.shadcn-root');

      console.debug('mode', mode, prevMode, 'userPrefersMode', userPrefersMode, prevUserPrefersMode, 'theme', theme, prevTheme, 'currentTheme', currentTheme, prevCurrentTheme);

      if (shadcnRoot && mode && (mode !== prevMode || userPrefersMode !== prevUserPrefersMode)) {
        currentThemeStore.setTheme(mode);
      } else if (
        prevCurrentTheme?.dark !== currentTheme?.dark ||
        userPrefersMode === 'system' ||
        // this condition handles some weird situation where the svelte-ux theme was changed, but that's not reflected in the value
        currentTheme.dark !== (mode === 'dark')
      ) {
        mode = currentTheme.dark ? 'dark' : 'light';
        setTimeout(() => {
          setMode(currentTheme.dark ? 'dark' : 'light');
        });
      }

      switch (mode) {
        case 'light': {
          document.documentElement.setAttribute('data-theme', 'light');
          shadcnRoot?.classList.remove('dark');
          shadcnRoot?.classList.add('light');
          break;
        }
        case 'dark': {
          document.documentElement.setAttribute('data-theme', 'dark');
          shadcnRoot?.classList.remove('light');
          shadcnRoot?.classList.add('dark');
          break;
        }
      }

      if (theme) {
        shadcnRoot?.setAttribute('data-theme', theme);
      } else {
        shadcnRoot?.removeAttribute('data-theme');
      }
    },
  );
</script>
