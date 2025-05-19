import {on} from 'svelte/events';
import type {Getter} from 'runed';
import {onDestroy} from 'svelte';

export interface BackHandlerConfig {
  /**
   * If true, the back handler will be added to the back stack.
   * If false, the back handler will be removed from the back stack.
   */
  addToStack: Getter<boolean>;
  onBack: () => void;
}


class BackHandler {
  #ignoreNextBack: boolean = false;
  static #backStack: BackHandler[] = [];
  constructor(private config: BackHandlerConfig) {
    $effect(() => {
      if (this.config.addToStack()) {
        if (BackHandler.#backStack.includes(this)) return;//already added
        BackHandler.#backStack.push(this);
        //add new history to ensure back doesn't pop some other state (like a url change)
        history.pushState(null, '');
      } else {
        this.remove();
      }
    });
    onDestroy(() => this.remove());
    onDestroy(on(window, 'popstate', () => {
      if (this.#ignoreNextBack) {
        this.#ignoreNextBack = false;
        return;
      }
      if (this.isNextBack) {
        //setTimeout ensures all popstate events are processed before we call the onBack callback
        setTimeout(() => {
          BackHandler.#backStack.pop();
          this.config.onBack();
        });
      }
    }));
  }

  get isNextBack() {
    return BackHandler.#backStack.at(-1) === this;
  }

  private remove() {
    const count = BackHandler.#backStack.length;
    BackHandler.#backStack = BackHandler.#backStack.filter(b => b !== this);
    if (count === BackHandler.#backStack.length) return;
    //if we removed the last back state, we need to remove the history entry that was pushed when we went here
    this.ignoreNextBack();
    history.back();
  }

  private ignoreNextBack() {
    for (let backHandler of BackHandler.#backStack) {
      backHandler.#ignoreNextBack = true;
    }
  }
}

export function useBackHandler(config: BackHandlerConfig) {
  new BackHandler(config);
}
