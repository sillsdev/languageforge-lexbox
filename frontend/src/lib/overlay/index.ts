import type {Action, ActionReturn} from 'svelte/action';
import {autoUpdate, computePosition, flip, offset} from '@floating-ui/dom';
import {browser} from '$app/environment';

type OverlayParams = { disabled?: boolean, closeClickSelector?: string } | undefined;
type OverlayAction = Action<HTMLElement, OverlayParams>;

class SharedOverlay {
  private containerElem: HTMLElement | undefined;
  private activeOverlay: { targetElem: HTMLElement; contentElem: HTMLElement } | undefined;
  private cleanupOverlay: (() => void) | undefined;


  constructor() {
    if (browser) document.addEventListener('click', this.closeHandler.bind(this));
  }

  public openOverlay(targetElem: HTMLElement, contentElem: HTMLElement): void {
    if (this.isActive(targetElem)) return;
    if (!this.containerElem) throw new Error('No overlay container has been provided');
    this.resetDom();
    this.activeOverlay = {targetElem, contentElem};
    this.containerElem.replaceChildren(contentElem);
    this.cleanupOverlay = autoUpdate(
      targetElem,
      this.containerElem,
      () => {
        if (!this.containerElem) return;
        void computePosition(targetElem, this.containerElem, {
          placement: 'bottom-end',
          middleware: [offset(2), flip()],
        }).then(({x, y}) => {
          if (!this.containerElem) return;
          Object.assign(this.containerElem.style, {
            left: `${x}px`,
            top: `${y}px`,
            display: '',
          });
        });
      });
  }

  public closeOverlay(): void {
    this.resetDom();
    this.activeOverlay = undefined;
  }

  private resetDom(): void {
    this.cleanupOverlay?.();
    this.cleanupOverlay = undefined;
    if (this.containerElem) {
      this.containerElem.style.display = 'none';
      this.containerElem?.replaceChildren();
    }
  }

  private closeHandler(event: MouseEvent): void {
    if (!this.activeOverlay) return;
    const eventPath = event.composedPath();
    if (!eventPath.includes(this.activeOverlay.targetElem) && !eventPath.includes(this.activeOverlay.contentElem)) {
      this.closeOverlay();
    }
  }

  public overlayContainer(element: HTMLElement): ActionReturn {
    if (this.containerElem) console.warn('Overlay container is already set');
    this.containerElem = element;
    this.containerElem.classList.add('overlay-container');
    this.resetDom();
    return {
      destroy: () => {
        if (this.containerElem !== element) return;
        this.closeOverlay();
        this.containerElem = undefined;
      }
    };
  }

  public isActive(elem: HTMLElement): boolean {
    return this.activeOverlay?.targetElem === elem;
  }

  public overlayTarget(...[target, params]: Parameters<OverlayAction>): ReturnType<OverlayAction> {
    const overlayTarget = new OverlayTarget(target,
      params?.disabled ?? false,
      params?.closeClickSelector ?? '',
      this);
    return {
      update: overlayTarget.update.bind(overlayTarget),
      destroy: overlayTarget.destroy.bind(overlayTarget),
    };
  }
}

class OverlayTarget implements ActionReturn<OverlayParams> {
  private contentElem: HTMLElement;
  private abortController = new AbortController();

  constructor(private targetElem: HTMLElement,
              private disabled: boolean,
              private closeClickSelector: string,
              private sharedOverlay: SharedOverlay) {
    this.contentElem = this.targetElem.querySelector<HTMLElement>('.overlay-content') as HTMLElement;
    if (!this.contentElem) throw new Error('Overlay target must have a child with class "overlay-content"');
    this.contentElem.remove();

    this.targetElem.addEventListener('click',
      () => this.openOverlay(),
      {signal: this.abortController.signal});

    // clicking on menu items should probably always close the overlay
    this.contentElem.addEventListener('click', (event) => {
      if (!this.closeClickSelector) return;
      if (event.target instanceof HTMLElement && event.target.closest(this.closeClickSelector)) {
        this.closeOverlay();
      }
    }, {signal: this.abortController.signal});
  }

  public destroy(): void {
    this.closeOverlay();
    this.abortController.abort();
  }

  public update(params: OverlayParams): void {
    if (params?.disabled !== undefined) this.disabled = params.disabled;
    if (this.disabled) this.closeOverlay();
    this.closeClickSelector = params?.closeClickSelector ?? '';
  }

  private isActive(): boolean {
    return this.sharedOverlay.isActive(this.targetElem);
  }

  private openOverlay(): void {
    if (!this.disabled) {
      this.sharedOverlay.openOverlay(this.targetElem, this.contentElem);
    }
  }

  private closeOverlay(): void {
    if (this.isActive()) {
      this.sharedOverlay.closeOverlay();
    }
  }
}


const sharedOverlay = new SharedOverlay();
export const overlay = sharedOverlay.overlayTarget.bind(sharedOverlay);
export const overlayContainer = sharedOverlay.overlayContainer.bind(sharedOverlay);
