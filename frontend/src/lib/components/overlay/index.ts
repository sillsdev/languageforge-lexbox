import type { Action, ActionReturn } from 'svelte/action';
import { autoUpdate, computePosition } from '@floating-ui/dom';
import { flip, offset } from '@floating-ui/dom';

import type { ActionParameters } from '$lib/svelte';
import { onDestroy } from 'svelte';

export const [overlayTarget, overlayContentWrapper] = createOnClickOverlay();

function createOnClickOverlay(): [Action, Action] {
  let targetElem: HTMLElement | undefined;
  let contentWrapperElem: HTMLElement | undefined;
  let contentElem: HTMLElement | undefined;

  let cleanup: (() => void) | undefined;

  function useTarget(...[node]: ActionParameters): ActionReturn {
    const targetContent = node.querySelector<HTMLElement>('.overlay-content') as HTMLElement;
    if (!targetContent) throw new Error('Overlay target must have a child with class "overlay-content"');

    targetContent.remove();

    function removeSelfFromOverlay(): void {
      if (targetElem === node) {
        targetElem = undefined;
        contentElem = undefined;
        targetContent.remove();
        updateOverlay();
      }
    }

    node.addEventListener('click', function (): void {
      if (targetElem === node) {
        removeSelfFromOverlay();
      }
      else {
        targetElem = node;
        contentElem = targetContent;
        updateOverlay();
      }
    });

    targetContent.addEventListener('focusout', function (event): void {
      if (targetElem !== node) return;
      else if (!event.relatedTarget || !node.contains(event.relatedTarget as Node)) removeSelfFromOverlay();
    });

    node.addEventListener('focusout', function (event): void {
      if (targetElem !== node) return;
      else if (!event.relatedTarget || !contentElem || !contentElem.contains(event.relatedTarget as Node)) removeSelfFromOverlay();
    });

    return {
      destroy(): void {
        removeSelfFromOverlay();
        targetContent.remove();
      }
    };
  }

  function useContentWrapper(...[node]: ActionParameters): ActionReturn {
    contentWrapperElem = node;
    return {
      destroy(): void {
        contentWrapperElem = undefined;
        updateOverlay();
      }
    };
  }

  function updateOverlay(): void {
    cleanup?.();
    if (!contentWrapperElem) return;

    if (targetElem && contentElem) {
      contentWrapperElem.replaceChildren(contentElem);
      cleanup = autoUpdate(
        targetElem,
        contentWrapperElem,
        () => {
          if (!targetElem || !contentWrapperElem) return;
          void computePosition(targetElem, contentWrapperElem, {
            placement: 'bottom-end',
            middleware: [offset(2), flip()]
          }).then(({ x, y }) => {
            if (!contentWrapperElem) return;
            Object.assign(contentWrapperElem.style, {
              left: `${x}px`,
              top: `${y}px`,
              display: '',
            });
          });
        });
    } else {
      contentWrapperElem.style.display = 'none';
    }
  }

  onDestroy(() => cleanup?.());

  return [useTarget, useContentWrapper];
}
