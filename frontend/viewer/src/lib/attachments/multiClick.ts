import type {Attachment} from 'svelte/attachments';

export type MultiClickOptions = {
  count: number;
  timeoutMs?: number;
  onTrigger: (event: PointerEvent) => void;
};

export function multiClick({
  count,
  timeoutMs = 500,
  onTrigger,
}: MultiClickOptions): Attachment<HTMLElement> {
  const safeCount = Math.max(1, Math.floor(count));
  const safeTimeoutMs = Math.max(0, Math.floor(timeoutMs));

  return (element) => {
    let clickCount = 0;
    let timeout: ReturnType<typeof setTimeout> | undefined;

    function reset() {
      clickCount = 0;
      clearTimeout(timeout);
      timeout = undefined;
    }

    function handleClick(event: PointerEvent) {
      clickCount++;
      clearTimeout(timeout);

      if (clickCount >= safeCount) {
        onTrigger(event);
        reset();
        return;
      }

      timeout = setTimeout(reset, safeTimeoutMs);
    }

    element.addEventListener('pointerup', handleClick);

    return () => {
      element.removeEventListener('pointerup', handleClick);
      reset();
    };
  };
}
