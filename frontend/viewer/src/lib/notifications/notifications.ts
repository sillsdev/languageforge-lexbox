import {toast} from 'svelte-sonner';

interface NotificationAction {
  label: string;
  callback: () => void;
}

export class AppNotification {

  public static display(message: string, type: 'success' | 'error' | 'info' | 'warning', timeout?: 'short' | 'long' | number, detail?: string) {
    if (!timeout || typeof timeout === 'number' && timeout <= 0) {
      timeout = Infinity;
    } else if (typeof timeout === 'string') {
      timeout = timeout === 'short' ? 5000 : 30000;
    }

    const toaster = type === 'info' ? toast : toast[type];
    toaster(message, {
      description: detail,
      duration: timeout,
    });
  }

  public static displayAction(message: string, type: 'success' | 'error' | 'info' | 'warning', action: NotificationAction) {
    const toaster = type === 'info' ? toast : toast[type];
    toaster(message, {
      duration: Infinity,
      action: {
        label: action.label,
        onClick: (event) => {
          event.preventDefault();
          action.callback();
        },
      },
    });
  }

  public static clear(): void {
    toast.dismiss();
  }

  private constructor(public message: string, public type: 'success' | 'error' | 'info' | 'warning', public action?: NotificationAction, public detail?: string) {
  }
}
