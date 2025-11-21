import {toast} from 'svelte-sonner';

interface NotificationAction {
  label: string;
  callback: () => {dismiss: boolean} | void;
}

type NotificationType = 'plain' | 'success' | 'error' | 'info' | 'warning';
type ToasterFn = typeof toast['info'];

// Infinity & MAX_SAFE_INT don't work reliably (only) for promise toasts 🤷
const INFINITY = 60_000 * 60 * 24 * 5; // 5 days
const NAMED_DURATIONS = {
  min: 3_000,
  short: 5_000,
  long: 30_000,
  infinite: INFINITY,
} as const;

type NotificationDuration = number | keyof typeof NAMED_DURATIONS;

export function pickDuration(timeout: NotificationDuration): number {
  if (typeof timeout === 'string') return NAMED_DURATIONS[timeout];
  return Math.max(timeout, NAMED_DURATIONS.min); // Apply min duration for UX
}

type ToastOptions = {
  type?: NotificationType;
  timeout?: NotificationDuration;
  description?: string;
}

type PromiseToastOptions<T> = Omit<ToastOptions, 'type'> &
  Pick<NonNullable<Parameters<typeof toast.promise<T>>[1]>, 'loading' | 'success' | 'error' | 'action'>;

function getToaster(type: NotificationType): ToasterFn {
  return type === 'plain' ? toast : toast[type];
}
export class AppNotification {

  public static display(message: string, options?: ToastOptions | NotificationType): void {
    const optionsObj = typeof options === 'string' ? {type: options} : options ?? {};
    const { type = 'plain', timeout = 'infinite', ...rest } = optionsObj;
    const duration = pickDuration(timeout);
    const toaster = getToaster(type);
    toaster(message, {
      duration,
      ...rest,
    });
  }

  public static error(message: string, detail?: string, clipboardText?: string): void {
    clipboardText ??= `${message}${detail ? `\n\n${detail}` : ''}`;
    toast.error(message, {
      duration: INFINITY,
      description: detail,
      action: {
        label: '',
        onClick: (event) => {
          event.preventDefault();
          void navigator.clipboard.writeText(clipboardText);
          const actionButton = event.currentTarget as HTMLButtonElement | null;
          actionButton?.classList.add('copied');
        },
      },
    });
  }

  public static promise<T>(promise: Promise<T>, options: PromiseToastOptions<T>) {
    const { timeout = 'infinite', ...rest } = options;
    return toast.promise(promise, { duration: pickDuration(timeout), ...rest });
  }

  public static displayAction(message: string, action: NotificationAction, options?: Omit<ToastOptions, 'timeout'> | NotificationType): void {
    const optionsObj = typeof options === 'string' ? {type: options} : options ?? {};
    const { type = 'plain', ...rest } = optionsObj;
    const toaster = getToaster(type);
    toaster(message, {
      duration: INFINITY,
      action: {
        label: action.label,
        onClick: (event) => {
          const result = action.callback();
          if (result?.dismiss) {
            return;
          }
          event.preventDefault();
        },
      },
      ...rest,
    });
  }

  public static clear(): void {
    toast.dismiss();
  }
}
