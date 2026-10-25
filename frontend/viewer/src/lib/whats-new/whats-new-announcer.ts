import {gt} from 'svelte-i18n-lingui';
import {PreferenceKey} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {AppNotification} from '$lib/notifications/notifications';
import {usePreferencesService} from '$lib/services/service-provider';
import {WHATS_NEW_VERSION, shouldAnnounceWhatsNew} from './whats-new';
import {openWhatsNew} from './whats-new-state.svelte';

let announced = false;

/**
 * Shows the "What's new" toast once after an update. Marks the version seen when the toast shows, not when
 * the dialog opens: the menu item is the way back, re-toasting every launch would be nagging.
 */
export async function announceWhatsNewIfNeeded(hasLocalProjects: boolean): Promise<void> {
  if (announced) return;
  announced = true;
  const preferences = usePreferencesService();
  const seenVersion = await preferences.get(PreferenceKey.WhatsNewSeenVersion);
  const announce = shouldAnnounceWhatsNew(seenVersion, hasLocalProjects);
  if (seenVersion !== WHATS_NEW_VERSION) await preferences.set(PreferenceKey.WhatsNewSeenVersion, WHATS_NEW_VERSION);
  if (!announce) return;
  AppNotification.displayAction(gt`FieldWorks Lite has been updated.`, {
    label: gt`What's new`,
    callback: () => {
      openWhatsNew();
      return {dismiss: true};
    },
  }, 'info');
}
