import type {Getter} from 'runed';
import {makeHistoryChange} from './history-orchestrator';
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
  key?: string;
}


class BackHandler {
  #ignoreNextBack: boolean = false;
  static #backStack: BackHandler[] = [];
  readonly #id = crypto.randomUUID();
  private get fullKey() {
    return `BackHandler-${this.#id}-${this.config.key ?? ''}`;
  };

  constructor(private config: BackHandlerConfig) {
    $effect(() => {
      if (this.config.addToStack()) {
        if (BackHandler.#backStack.includes(this)) return;//already added
        BackHandler.#backStack.push(this);
        //add new history to ensure back doesn't pop some other state (like a url change)
        void makeHistoryChange(() => history.pushState({
          backHandler: true,
          key: this.config.key,
          id: this.#id,
        }, ''), {
          key: this.fullKey,
          isTeardown: false,
        });
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
    if (count !== BackHandler.#backStack.length) {
      //if we removed the last back state, we need to remove the history entry that was pushed
      const currentLocation = location.href;
      void makeHistoryChange(() => {
        //navigation triggered since remove was called, we don't want to go back now as that would not undo our history but a navigation event
        if (currentLocation !== location.href) {
          console.warn(`${this.fullKey}: remove called while navigating, ignoring, history entry not removed. Navigation should happen after remove is called, eg: after closing the modal which triggers a navigation.`);
          return;
        }
        if (!this.isOnTopOfHistoryStack()) {
          console.warn(`${this.fullKey}: remove called but not on top of history stack, ignoring, history entry not removed.`);
          return;
        }
        this.ignoreNextBack();
        history.back();
        return { triggersPopstate: true };
      }, {
        key: this.fullKey,
        isTeardown: true,
      });
    }
  }

  private ignoreNextBack() {
    for (const backHandler of BackHandler.#backStack) {
      backHandler.#ignoreNextBack = true;
    }
  }

  private isOnTopOfHistoryStack() {
    const state = history.state as unknown;
    if (typeof state !== 'object' || !state) return false;
    return 'backHandler' in state && state.backHandler === true;
  }
}

export function useBackHandler(config: BackHandlerConfig) {
  new BackHandler(config);
}
