import {AppNotification} from '$lib/notifications/notifications';
import {gt} from 'svelte-i18n-lingui';
import {useAppLauncherService} from './app-launcher-service';

export async function openUrl(url: string) {
  const appLauncher = useAppLauncherService();
  let opened = false;
  if (appLauncher) {
    opened = await appLauncher.tryOpen(url);
  }

  if (!opened) {
    try {
      window.open(url, '_blank');
    } catch {
      AppNotification.display(gt`Failed to open URL: ${url}`, 'warning');
    }
  }
}
