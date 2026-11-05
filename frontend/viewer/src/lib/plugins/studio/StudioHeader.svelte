<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useStudio, type StudioStatus} from './plugin-studio.svelte';

  interface Props {
    title: string;
  }

  const {title}: Props = $props();
  const studio = useStudio();
  const draft = studio.draft;

  const statusChips = $derived<Record<StudioStatus, {label: string; tone: 'muted' | 'ok' | 'warn'}>>({
    added: {label: $t`In this project`, tone: 'ok'},
    example: {label: $t`Example, not in this project`, tone: 'muted'},
    'example-customized': {label: $t`Your version, not added yet`, tone: 'muted'},
    new: {label: $t`Draft, only you can see it`, tone: 'muted'},
    unsaved: {label: $t`Unsaved changes`, tone: 'warn'},
  });
  const statusChip = $derived(studio.status ? statusChips[studio.status] : undefined);

  async function addToProject() {
    const added = await studio.addToProject();
    AppNotification.display($t`"${added.name}" was added for everyone on this project.`, {timeout: 'short', type: 'success'});
  }

  async function saveEdit() {
    await studio.saveEdit();
    AppNotification.display($t`Saved for the team.`, {timeout: 'short', type: 'success'});
  }
</script>

<header class="flex flex-wrap items-center gap-x-3 gap-y-2 border-b px-4 py-2.5">
  <nav aria-label={$t`Breadcrumb`} class="flex min-w-0 items-center gap-2">
    <Button variant="link" class="px-0" onclick={() => void studio.leave()}>{$t`Plugins`}</Button>
    <span class="text-muted-foreground" aria-hidden="true">/</span>
    {#if studio.mode === 'example'}
      <span class="text-sm text-muted-foreground max-sm:hidden">{$t`Examples`}</span>
      <span class="text-muted-foreground max-sm:hidden" aria-hidden="true">/</span>
    {/if}
    <h1 class="truncate font-semibold">{title}</h1>
  </nav>
  {#if statusChip}
    <span
      class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
      class:bg-muted={statusChip.tone === 'muted'}
      class:text-muted-foreground={statusChip.tone === 'muted'}
      class:bg-green-100={statusChip.tone === 'ok'}
      class:text-green-800={statusChip.tone === 'ok'}
      class:bg-amber-100={statusChip.tone === 'warn'}
      class:text-amber-800={statusChip.tone === 'warn'}
    >
      {statusChip.label}
    </span>
  {/if}
  <div class="grow"></div>
  <div class="flex flex-wrap items-center gap-2">
    {#if studio.added}
      {@const added = studio.added}
      <span class="text-sm font-medium text-green-700 dark:text-green-400">{$t`Added for the team`}</span>
      <Button variant="outline" icon="i-mdi-play" onclick={() => studio.openPlugin(added.id)}>{$t`Run it`}</Button>
      <Button onclick={studio.goToPlugins}>{$t`Back to plugins`}</Button>
    {:else if studio.mode === 'example'}
      <Button variant="outline" onclick={() => void studio.leave()}>{$t`Close`}</Button>
      <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={studio.saving} onclick={() => void addToProject()}>
        {draft.customized ? $t`Add my version` : $t`Add to project`}
      </Button>
    {:else if studio.mode === 'new'}
      <Button variant="outline" onclick={() => void studio.leave()}>{$t`Discard`}</Button>
      <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={studio.saving} onclick={() => void addToProject()}>
        {$t`Add to project`}
      </Button>
    {:else}
      <Button variant="outline" disabled={!draft.dirty} onclick={() => studio.discardChanges()}>{$t`Discard`}</Button>
      <Button icon="i-mdi-content-save" disabled={!draft.canSave} loading={studio.saving} onclick={() => void saveEdit()}>
        {$t`Save for the team`}
      </Button>
    {/if}
  </div>
</header>
