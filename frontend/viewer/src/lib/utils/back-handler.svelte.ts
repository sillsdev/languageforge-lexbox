import {awaitPopstate, queueHistoryChange} from './history';

import type {Getter} from 'runed';
import {on} from 'svelte/events';
import {onDestroy} from 'svelte';

export interface BackHandlerConfig {
  /**
   * If true, the back handler will be added to the back stack.
   * If false, the back handler will be removed from the back stack.
   */
  addToStack: Getter<boolean>;
  onBack: () => void;
  /**
   * Identifies the back handler, used for warnings and debugging.
   */
  key: string;
}

class BackHandler {
  static #ignorePopstate = false;
  static #backStack: BackHandler[] = [];
  private get fullKey() {
    return `BackHandler-${this.config.key}`;
  };

  constructor(private config: BackHandlerConfig) {
    $effect(() => {
      if (this.config.addToStack()) {
        if (BackHandler.#backStack.includes(this)) return;//already added
        BackHandler.#backStack.push(this);
        //add new history to ensure back doesn't pop some other state (like a url change)
        void queueHistoryChange(() => history.pushState({
          backHandler: true,
          key: this.config.key,
        }, ''), this.fullKey);
      } else {
        this.remove();
      }
    });
    onDestroy(() => this.remove());
    onDestroy(on(window, 'popstate', () => {
      if (BackHandler.#ignorePopstate) return;

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
    if (!this.isOnBackStack()) return;

    BackHandler.#backStack = BackHandler.#backStack.filter(b => b !== this);

    if (!BackHandler.isOnTopOfHistoryStack()) {
      console.warn(`${this.fullKey}: remove called but BackHandler is not on top of history stack, ignoring, history entry not removed.`);
      return;
    }

    // we removed a back handler, so we need to remove a history entry too
    const currentLocation = location.href;
    void queueHistoryChange(() => {
      //navigation triggered since remove was called, we don't want to go back now as that would not undo our history but a navigation event
      if (currentLocation !== location.href || !BackHandler.isOnTopOfHistoryStack()) {
        console.warn(
          `${this.fullKey}: remove called while navigating, ignoring, history entry not removed. Navigation should happen after remove is called, eg: after closing the modal which triggers a navigation.`,
          `(${currentLocation}, ${location.href}, ${BackHandler.isOnTopOfHistoryStack()})`
        );
        return;
      }
      BackHandler.#ignorePopstate = true;
      history.back();
      void awaitPopstate().finally(() => {
        setTimeout(() => { //setTimeout ensures all popstate events are processed before we stop ignoring
          BackHandler.#ignorePopstate = false;
        });
      });
      return { triggeredPopstate: true };
    }, this.fullKey);
  }

  private isOnBackStack() {
    return BackHandler.#backStack.includes(this);
  }

  private static isOnTopOfHistoryStack() {
    const state = history.state as unknown;
    if (typeof state !== 'object' || !state) return false;
    return 'backHandler' in state && state.backHandler === true;
  }
}

export function useBackHandler(config: BackHandlerConfig) {
  new BackHandler(config);
}
