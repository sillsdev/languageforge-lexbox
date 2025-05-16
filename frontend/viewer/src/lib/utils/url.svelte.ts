import {createSubscriber} from 'svelte/reactivity';
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

  public set current(value: string) {
    if (value === this.#current) return;
    const currentUrl = new URL(document.location.href);
    const isDefault = value === this.defaultValue;
    if (isDefault) {
      currentUrl.searchParams.delete(this.config.key);
    } else {
      currentUrl.searchParams.set(this.config.key, value);
    }
    if (this.config.replaceOnDefaultValue && isDefault) {
      if (history.state?.push === this.config.key) {
        //the last history event was push by us so we need to just go back otherwise the next back will do nothing
        history.go(-1);
      } else {
        history.replaceState(null, '', currentUrl.href);
      }
    } else if (this.config.allowBack) {
      history.pushState({push: this.config.key}, '', currentUrl.href);
    } else {
      history.replaceState(null, '', currentUrl.href);
    }
    //history events don't trigger popstate, so we need to set the value directly
    this.#current = value;
  }

  constructor(private config: QueryParamStateConfig, private defaultValue: string = '') {
    const location = useLocation();

    this.#current = this.readUrlValue();
    //ensures that we only subscribe to popstate if current is being watched/used in an $effect
    this.#subscribe = createSubscriber(update => {
      const off = location.subscribe(() => {
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
