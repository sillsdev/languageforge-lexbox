import type {Action, ActionReturn} from 'svelte/action';
import {autoUpdate, computePosition, flip, offset, type Placement} from '@floating-ui/dom';

import {browser} from '$app/environment';

export { default as OverlayContainer } from './OverlayContainer.svelte';
type OverlayOpenCallback = (open: boolean) => void;
type OverlayParams = { disabled?: boolean, closeClickSelector?: string, onOverlayOpen?: OverlayOpenCallback } | undefined;
type OverlayAction = Action<HTMLElement, OverlayParams>;

class SharedOverlay {
  private containerStack: HTMLElement[] = [];
  private containerElem: HTMLElement | undefined;
  private activeOverlay: OverlayTarget | undefined;
  private cleanupOverlay: (() => void) | undefined;


  constructor() {
    if (browser) document.addEventListener('click', this.closeHandler.bind(this));
  }

  public openOverlay(target: OverlayTarget, placement: Placement): void {
    if (this.isActive(target.targetElem)) return;
    this.closeOverlay();
    this.openOverlayInternal(target, placement);
  }

  private openOverlayInternal(target: OverlayTarget, placement: Placement): void {
    if (!this.containerElem) throw new Error('No overlay container has been provided');
    this.activeOverlay = target;
    target.dispatchOpenEvent(true)
    this.containerElem.replaceChildren(target.contentElem);
    this.cleanupOverlay = autoUpdate(
      target.targetElem,
      this.containerElem,
      () => {
        if (!this.containerElem) return;
        void computePosition(target.targetElem, this.containerElem, {
          placement,
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
    if (this.activeOverlay) {
      this.activeOverlay.dispatchOpenEvent(false);
      this.activeOverlay = undefined;
    }
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
    if (this.containerElem) {
      this.containerStack.push(this.containerElem);
      this.closeOverlay();
    }
    this.containerElem = element;
    this.containerElem.classList.add('overlay-container');
    this.resetDom();
    return {
      destroy: () => {
        this.containerStack = this.containerStack.filter((elem) => elem !== element);
        if (this.containerElem !== element) return;
        this.closeOverlay();
        this.containerElem = this.containerStack.pop();
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
      params?.onOverlayOpen ?? (() => {}),
      this);
    return {
      update: overlayTarget.update.bind(overlayTarget),
      destroy: overlayTarget.destroy.bind(overlayTarget),
    };
  }
}

class OverlayTarget implements ActionReturn<OverlayParams> {
  public readonly contentElem: HTMLElement;
  private abortController = new AbortController();
  private useInputConfig = false;

  constructor(public readonly targetElem: HTMLElement,
              private disabled: boolean,
              private closeClickSelector: string,
              private onOverlayOpen: OverlayOpenCallback,
              private sharedOverlay: SharedOverlay) {
    this.contentElem = this.targetElem.querySelector<HTMLElement>('.overlay-content') as HTMLElement;
    if (!this.contentElem) throw new Error('Overlay target must have a child with class "overlay-content"');
    this.contentElem.remove();

    const inputElem = this.targetElem.matches('input') ? this.targetElem : this.targetElem.querySelector('input');
    this.useInputConfig = !!inputElem;

    if (this.useInputConfig) {
      this.targetElem.addEventListener('focusin',
        () => !this.isActive() && this.openOverlay(),
        { signal: this.abortController.signal });
      inputElem?.addEventListener('input',
        () => !this.isActive() && this.openOverlay(),
        { signal: this.abortController.signal });
      inputElem?.addEventListener('keydown',
        (e) => this.isActive() && e.key === 'Enter' && this.closeOverlay(),
        { signal: this.abortController.signal });
      this.targetElem.addEventListener('focusout',
        () => {
          // When clicking on an element in the content, the focus first goes to the body and only then to the element.
          setTimeout(() => {
            if (this.isActive() && !this.contentElem.contains(document.activeElement)) this.closeOverlay();
          });
        },
        {signal: this.abortController.signal});
    } else {
      this.targetElem.addEventListener('click',
        () => this.isActive() ? this.closeOverlay() : this.openOverlay(),
        {signal: this.abortController.signal});
    }

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
      this.sharedOverlay.openOverlay(this,
        this.useInputConfig ? 'bottom-start' : 'bottom-end');
    }
  }

  private closeOverlay(): void {
    if (this.isActive()) {
      this.sharedOverlay.closeOverlay();
    }
  }

  public dispatchOpenEvent(open: boolean): void {
    this.onOverlayOpen(open);
  }
}


const sharedOverlay = new SharedOverlay();
export const overlay = sharedOverlay.overlayTarget.bind(sharedOverlay);
export const overlayContainer = sharedOverlay.overlayContainer.bind(sharedOverlay);
