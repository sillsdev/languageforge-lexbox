<script lang="ts">
  import * as Tabs from '$lib/components/ui/tabs';
  import {t} from 'svelte-i18n-lingui';
  import {onDestroy, untrack} from 'svelte';
  import {navigate, useRouter} from 'svelte-routing';
  import {UserProjectRole} from '$lib/dotnet-types';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {setLeaveGuard} from '$lib/utils/leave-guard';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import type {StudioMode} from './plugin-studio-draft.svelte';
  import {PluginStudio, setStudio} from './plugin-studio.svelte';
  import StudioHeader from './StudioHeader.svelte';
  import StudioPreview from './StudioPreview.svelte';
  import StudioSidePanel from './StudioSidePanel.svelte';
  import StudioNotice from './StudioNotice.svelte';
  import InternetPromptDialog from './InternetPromptDialog.svelte';
  import LeaveDialog from './LeaveDialog.svelte';

  interface Props {
    mode: StudioMode;
    /** Edit mode: the plugin being edited. */
    pluginId?: string;
    /** Example mode: the example being tried. */
    exampleKey?: string;
  }

  const {mode, pluginId, exampleKey}: Props = $props();

  const projectContext = useProjectContext();
  const {base} = useRouter();

  // Local-only projects have no role info; there the user owns their data anyway.
  const canManage = $derived(!projectContext.projectData || projectContext.projectData.role === UserProjectRole.Manager);

  function goToPlugins() {
    navigate(`${$base.uri}/plugins`);
  }

  // The route keys this view, so the mode and the plugin/example it is about never change.
  const studio = untrack(() => new PluginStudio({
    mode,
    pluginId,
    exampleKey,
    pluginService: usePluginService(),
    project: projectContext,
    goToPlugins,
    openPlugin: (id) => navigate(`${$base.uri}/plugins/${id}`),
  }));
  setStudio(studio);
  onDestroy(() => studio.destroy());

  // ---- Leaving: the sidebar, browser/Android back and closing the window all ask first ----

  $effect(() => setLeaveGuard(() => studio.confirmLeave()));

  // Staying re-arms the back guard.
  let backGuardVersion = $state(0);
  useBackHandler({
    addToStack: () => {
      backGuardVersion; // eslint-disable-line @typescript-eslint/no-unused-expressions
      return studio.hasUnsavedWork;
    },
    onBack: () => void studio.confirmLeave().then(leave => {
      if (leave) goToPlugins();
      else backGuardVersion++;
    }),
    key: 'plugin-studio',
  });

  function onBeforeUnload(event: BeforeUnloadEvent) {
    if (studio.hasUnsavedWork) event.preventDefault();
  }

  const title = $derived(studio.name ?? (mode === 'new' ? $t`New plugin` : $t`Plugin`));
</script>

<svelte:window onbeforeunload={onBeforeUnload} />

{#if !canManage}
  <StudioNotice icon="i-mdi-lock-outline" message={$t`Only project managers can add or change plugins.`} onBack={goToPlugins} />
{:else if studio.loadState === 'missing'}
  <StudioNotice icon="i-mdi-puzzle-remove" message={mode === 'example' ? $t`This example doesn't exist.` : $t`This plugin no longer exists.`} onBack={goToPlugins} />
{:else if studio.loadState === 'offline'}
  <StudioNotice icon="i-mdi-cloud-off-outline" message={$t`You're offline and this plugin's file isn't on this device yet. Connect and try again.`} onBack={goToPlugins} />
{:else if studio.loadState === 'error'}
  <StudioNotice icon="i-mdi-alert-circle-outline" message={$t`Couldn't load the plugin file.`} onBack={goToPlugins} />
{:else}
  <div class="h-full flex flex-col bg-background" data-testid="plugin-studio">
    <StudioHeader {title} />

    {#if IsMobile.value}
      <Tabs.Root bind:value={studio.mobilePane} class="px-3 pt-2">
        <Tabs.List class="w-full">
          <Tabs.Trigger value="preview" class="flex-1">{$t`Preview`}</Tabs.Trigger>
          <Tabs.Trigger value="edit" class="flex-1">{$t`Edit`}</Tabs.Trigger>
        </Tabs.List>
      </Tabs.Root>
    {/if}

    <div class="flex min-h-0 grow">
      <section
        aria-label={$t`Preview`}
        class="flex min-w-0 grow flex-col bg-muted/50"
        class:hidden={IsMobile.value && studio.mobilePane !== 'preview'}
      >
        <StudioPreview {title} />
      </section>

      <aside
        aria-label={$t`Plugin`}
        class="flex w-full shrink-0 flex-col border-s md:w-[440px]"
        class:hidden={IsMobile.value && studio.mobilePane !== 'edit'}
      >
        <StudioSidePanel />
      </aside>
    </div>
  </div>
{/if}

<InternetPromptDialog />
<LeaveDialog />
