import {createSubscriber} from 'svelte/reactivity';
import {makeHistoryChange} from './history-orchestrator';
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

  #waitingForHistoryChange: boolean = false;

  public set current(value: string) {
    if (value === this.#current) return;
    //history events don't trigger popstate, so we need to set the value directly
    this.#current = value;
    void this.updateHistory();
  }

  private async updateHistory(): Promise<void> {
    const isDefault = this.#current === this.defaultValue;
    const isTeardown = this.config.replaceOnDefaultValue && isDefault;
    this.#waitingForHistoryChange = true;
    await makeHistoryChange(() => {
      const currentUrl = new URL(document.location.href);
      if (isDefault) {
        currentUrl.searchParams.delete(this.config.key);
      } else {
        currentUrl.searchParams.set(this.config.key, this.#current);
      }
      if (isTeardown) {
        if (this.config.allowBack) {
          if (!this.isOnTopOfHistoryStack()) {
            console.warn(`${this.fullKey}: wanted to pop history, but not on top of history stack, ignoring, history entry not removed.`);
            return;
          }
          //the last history event was pushed by us so we need to just go back otherwise the next back will do nothing
          history.go(-1);
          return { triggeredPopstate: true };
        } else {
          history.replaceState(null, '', currentUrl.href);
        }
      } else {
        if (this.config.allowBack) {
          console.log(`Pushing history state for key "${this.fullKey}" with value "${this.#current}" (${currentUrl.href}).`);
          history.pushState({pushKey: this.fullKey}, '', currentUrl.href);
        } else {
          history.replaceState(null, '', currentUrl.href);
        }
      }
    }, this.fullKey);
    this.#waitingForHistoryChange = false;
  }

  private isOnTopOfHistoryStack() {
    const state = history.state as unknown;
    const pushKey = state && typeof state === 'object' && 'pushKey' in state ? state.pushKey as string : undefined;
    return pushKey === this.fullKey;
  }

  readonly #id = crypto.randomUUID().split('-')[0];
  private get fullKey() {
    return `QueryParamState-${this.#id}-${this.config.key ?? ''}`;
  };

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
    return new URL(document.location.href).searchParams.get(this.config.key) ?? this.defaultValue;
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
