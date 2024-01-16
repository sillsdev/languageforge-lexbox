import type { Action, ActionReturn } from 'svelte/action';
import { autoUpdate, computePosition } from '@floating-ui/dom';
import { flip, offset } from '@floating-ui/dom';

export const { overlayTarget: overlay, overlayContainer } = buildSharedClickOverlay();

type OverlayParams = { disabled?: boolean } | undefined;
type OverlayAction = Action<HTMLElement, OverlayParams>;

function buildSharedClickOverlay(): { overlayTarget: OverlayAction; overlayContainer: Action } {
  let containerElem: HTMLElement | undefined;
  let activeOverlay: { targetElem: HTMLElement; contentElem: HTMLElement } | undefined;

  let cleanup: (() => void) | undefined;

  function updateOverlay(): void {
    resetDom();
    if (!containerElem) throw new Error('No overlay container has been provided');

    if (activeOverlay) {
      const { targetElem, contentElem } = activeOverlay;
      containerElem.replaceChildren(contentElem);
      cleanup = autoUpdate(
        targetElem,
        containerElem,
        () => {
          if (!targetElem || !containerElem) return;
          void computePosition(targetElem, containerElem, {
            placement: 'bottom-end',
            middleware: [offset(2), flip()],
          }).then(({ x, y }) => {
            if (!containerElem) return;
            Object.assign(containerElem.style, {
              left: `${x}px`,
              top: `${y}px`,
              display: '',
            });
          });
        });
    }
  }

  function resetDom(): void {
    cleanup?.();
    if (containerElem) {
      containerElem.style.display = 'none';
      containerElem?.replaceChildren();
    }
  }

  function closeOverlay(): void {
    resetDom();
    activeOverlay = undefined;
  }

  function overlayContainer(...[element]: Parameters<Action>): ActionReturn {
    if (containerElem) console.warn('Overlay container is already set');
    containerElem = element;
    containerElem.classList.add('overlay-container');
    resetDom();
    return {
      destroy(): void {
        closeOverlay();
      }
    };
  }

  function overlayTarget(...[targetElem, params]: Parameters<OverlayAction>): ReturnType<OverlayAction> {
    let disabled = params?.disabled ?? false;
    const contentElem = targetElem.querySelector<HTMLElement>('.overlay-content') as HTMLElement;
    if (!contentElem) throw new Error('Overlay target must have a child with class "overlay-content"');

    contentElem.remove();

    function isActive(): boolean {
      return activeOverlay?.targetElem === targetElem;
    }

    function deactivate(): void {
      if (isActive()) {
        closeOverlay();
      }
    }

    targetElem.addEventListener('click', function (): void {
      if (isActive()) {
        deactivate();
      } else if (!disabled) {
        activeOverlay = { targetElem, contentElem };
        updateOverlay();
      }
    });

    // clicking on menu items should probably always close the overlay
    contentElem.querySelectorAll('.menu li').forEach((item) => {
      item.addEventListener('click', () => {
        deactivate();
      });
    });

    document.addEventListener('click', function (event): void {
      if (isActive()) {
        const eventPath = event.composedPath();
        if (!eventPath.includes(targetElem) && !eventPath.includes(contentElem)) deactivate();
      }
    });

    return {
      destroy(): void {
        deactivate();
      },
      update(newParams): void {
        if (newParams?.disabled !== undefined) disabled = newParams.disabled;
        if (disabled) deactivate();
      }
    };
  }

  return {
    overlayContainer,
    overlayTarget,
  };
}
