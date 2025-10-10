import {SvelteURL, createSubscriber} from 'svelte/reactivity';

import {queueHistoryChange} from './history';
import {useLocation} from 'svelte-routing';

export interface QueryParamStateConfig {
  key: string;
  allowBack?: boolean;
  replaceOnDefaultValue?: boolean;
}

/**
 *
 * A reactive query parameter state
 * it reacts to users pressing back and svelte router changes
 */
export class QueryParamState {
  #subscribe: ReturnType<typeof createSubscriber>;
  #current: string = $state('');
  public get current(): string {
    this.#subscribe();
    return this.#current;
  }

  private get fullKey() {
    return `QueryParamState-${this.config.key}`;
  };

  #waitingForHistoryChange: boolean = false;

  public set current(value: string) {
    if (value === this.#current) return;
    //history events don't trigger popstate, so we need to set the value directly
    this.#current = value;
    void this.updateHistory();
  }

  private async updateHistory(): Promise<void> {
    const isDefault = this.#current === this.defaultValue;
    this.#waitingForHistoryChange = true;
    await queueHistoryChange(() => {
      try {
        const currentUrl = new SvelteURL(document.location.href);
        if (isDefault) {
          currentUrl.searchParams.delete(this.config.key);
        } else {
          currentUrl.searchParams.set(this.config.key, this.#current);
        }
        if (this.config.replaceOnDefaultValue && isDefault) {
          if (this.config.allowBack) {
            if (!this.isOnTopOfHistoryStack()) {
              console.warn(`${this.fullKey}: wanted to pop history, but not on top of history stack. Pushing new state instead so change is applied.`);
              history.pushState(null, '', currentUrl.href);
              return;
            }
            //the last history event was pushed by us so we need to just go back otherwise the next back will do nothing
            history.go(-1);
            return {triggeredPopstate: true};
          } else {
            history.replaceState(null, '', currentUrl.href);
          }
        } else {
          if (this.config.allowBack) {
            history.pushState({pushKey: this.fullKey}, '', currentUrl.href);
          } else {
            history.replaceState(null, '', currentUrl.href);
          }
        }
      } finally {
        this.#waitingForHistoryChange = false;
      }
    }, this.fullKey);
  }

  private isOnTopOfHistoryStack() {
    const state = history.state as unknown;
    const pushKey = state && typeof state === 'object' && 'pushKey' in state ? state.pushKey as string : undefined;
    return pushKey === this.fullKey;
  }

  constructor(private config: QueryParamStateConfig, private defaultValue: string = '') {
    const location = useLocation();

    this.#current = this.readUrlValue();
    //ensures that we only subscribe to popstate if current is being watched/used in an $effect
    this.#subscribe = createSubscriber(update => {
      const off = location.subscribe(() => {
        if (this.#waitingForHistoryChange) {
          //our history change is still in progress, so we don't currently trust the url
          return;
        }
        this.#current = this.readUrlValue();
        update();
      });
      return () => off();
    });
  }

  private readUrlValue(): string {
    return new SvelteURL(document.location.href).searchParams.get(this.config.key) ?? this.defaultValue;
  }
}

export class QueryParamStateBool {
  #stringState: QueryParamState;

  get current(): boolean {
    return this.#stringState.current === 'true';
  }

  set current(value: boolean) {
    this.#stringState.current = value.toString();
  }

  constructor(config: QueryParamStateConfig, defaultValue: boolean = false) {
    this.#stringState = new QueryParamState(config, defaultValue.toString());
  }
}
