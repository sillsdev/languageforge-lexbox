<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import type {PluginPermission} from '../../plugin-api-types';
  import type {ManifestChanges} from '../../plugin-manifest';
  import {useStudio} from '../plugin-studio.svelte';

  interface Props {
    review: {changes: ManifestChanges; sizeBefore: number};
  }

  const {review}: Props = $props();
  const studio = useStudio();

  const permissionWarnings: Record<PluginPermission, string> = $derived({
    internet: $t`The new version asks to use the internet`,
    edit: $t`The new version asks to change the dictionary`,
  });

  const otherChanges = $derived.by(() => {
    const {added, removed} = review.changes;
    return [
      ...added.permissions.filter(permission => studio.keptPermissions.includes(permission))
        .map(permission => permission === 'internet' ? $t`Now uses the internet` : $t`Now can change the dictionary`),
      ...removed.permissions.map(permission => permission === 'internet' ? $t`No longer uses the internet` : $t`No longer changes the dictionary`),
      ...added.contexts.map(() => $t`Now runs from an entry's menu`),
      ...removed.contexts.map(() => $t`No longer runs from an entry's menu`),
      ...added.requires.map(feature => $t`Now needs ${feature}, which not every project has`),
      ...removed.requires.map(feature => $t`No longer needs ${feature}`),
    ];
  });
</script>

<div class="space-y-2" data-testid="plugin-version-review">
  <h3 class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{$t`This version`}</h3>
  <div class="rounded-md bg-muted px-3 py-2 text-sm">
    {#if review.changes.added.permissions.length === 0}
      {$t`${review.sizeBefore} KB to ${studio.sizeKb} KB, no new permissions`}
    {:else}
      {$t`${review.sizeBefore} KB to ${studio.sizeKb} KB`}
    {/if}
  </div>
  {#each studio.newPermissions as permission (permission)}
    <div class="space-y-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm" role="alert">
      <div class="font-semibold text-destructive">{permissionWarnings[permission]}</div>
      <p>{$t`The version before it didn't. If you didn't ask for this, have the AI take it out.`}</p>
      <div class="flex flex-wrap gap-2">
        <Button variant="outline" size="sm" loading={studio.buildingChangePrompt} onclick={() => void studio.askToRemove(permission)}>
          {$t`Ask the AI to remove it`}
        </Button>
        <Button variant="ghost" size="sm" onclick={() => studio.keepPermission(permission)}>{$t`Keep it`}</Button>
      </div>
    </div>
  {/each}
  {#if studio.followUpDownloaded}
    <p class="rounded-md bg-muted px-3 py-2 text-sm">
      {$t`Follow-up prompt downloaded. Attach it in the same AI chat, then paste its reply below.`}
    </p>
  {/if}
  {#if otherChanges.length}
    <ul class="list-disc space-y-1 ps-5 text-sm">
      {#each otherChanges as line (line)}
        <li>{line}</li>
      {/each}
    </ul>
  {/if}
  {#if studio.writeTrustResets}
    <p class="rounded-md bg-muted px-3 py-2 text-sm">
      {$t`Saving turns off "Always allow" on this device, so the new version asks before its first change.`}
    </p>
  {/if}
</div>
