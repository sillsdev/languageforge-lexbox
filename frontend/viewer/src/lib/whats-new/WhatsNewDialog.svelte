<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import ReleaseNotesLink from '$lib/updates/ReleaseNotesLink.svelte';
  import type {IconClass} from '$lib/icon-class';
  import {currentWhatsNewEntries, type WhatsNewEntryId} from './whats-new';
  import {whatsNew} from './whats-new-state.svelte';

  type Entry = {icon: IconClass; title: string; description: string; audience?: string};

  const currentEntries = currentWhatsNewEntries();

  const entries = $derived<Record<WhatsNewEntryId, Entry>>({
    comments: {
      icon: 'i-mdi-comment-text-outline',
      title: $t`Comments on entries`,
      description: $t`Ask a question, flag a problem or agree on a change without editing the entry. Comments from teammates appear as soon as they sync, and you can filter the list to entries with unread comments.`,
    },
    'commenter-role': {
      icon: 'i-mdi-account-badge-outline',
      title: $t`Commenter role`,
      audience: $t`For project managers`,
      description: $t`On Lexbox you can now give someone the Commenter role: they can read the dictionary and comment on it, but not edit it.`,
    },
    'flex-collation': {
      icon: 'i-mdi-sort-alphabetical-variant',
      title: $t`Sorting matches FieldWorks`,
      description: $t`Entries are sorted with the same custom sort order (collation) that is set up for each writing system in FieldWorks.`,
    },
    pictures: {
      icon: 'i-mdi-image-multiple-outline',
      title: $t`Pictures on meanings`,
      description: $t`Add pictures to a meaning, take them with your device's camera, and use the media manager to download media for offline use.`,
    },
    'error-reports': {
      icon: 'i-mdi-shield-bug-outline',
      title: $t`Error reports`,
      description: $t`When something goes wrong, FieldWorks Lite now sends an anonymous error report along with the usage data it already collects. Dictionary text and personal details are removed first. You can turn this off under Privacy.`,
    },
    'project-upgrade': {
      icon: 'i-mdi-database-refresh-outline',
      title: $t`Dictionaries are converted once`,
      description: $t`Dictionaries that were already on this device are converted to a new format the first time you edit or sync them after this update. FieldWorks Lite may seem frozen for a few minutes on a large dictionary. Keep the app open until it finishes.`,
    },
  });
</script>

<ResponsiveDialog bind:open={whatsNew.open} title={$t`What's new in FieldWorks Lite`}>
  {#if currentEntries.length === 0}
    <p class="text-muted-foreground">{$t`Nothing new in the last few months.`}</p>
  {/if}
  <ul class="flex flex-col gap-4">
    {#each currentEntries as id (id)}
      {@const entry = entries[id]}
      <li class="flex gap-3">
        <Icon icon={entry.icon} class="mt-0.5 text-muted-foreground" />
        <div class="flex flex-col gap-0.5">
          <p class="font-medium">{entry.title}</p>
          {#if entry.audience}
            <p class="text-xs text-muted-foreground">{entry.audience}</p>
          {/if}
          <p class="text-sm text-muted-foreground">{entry.description}</p>
        </div>
      </li>
    {/each}
  </ul>
  <div class="flex justify-center mt-4">
    <ReleaseNotesLink />
  </div>
</ResponsiveDialog>
