import {createSubscriber} from 'svelte/reactivity';
import {useLocation} from 'svelte-routing';

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
    if (value === this.defaultValue) {
      currentUrl.searchParams.delete(this.key);
    } else {
      currentUrl.searchParams.set(this.key, value);
    }
    if (this.allowBack) {
      history.pushState(null, '', currentUrl.href);
    } else {
      history.replaceState(null, '', currentUrl.href);
    }
    //history events don't trigger popstate, so we need to set the value directly
    this.#current = value;
  }

  constructor(private key: string, private allowBack: boolean = false, private defaultValue: string = '') {
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
    return new URL(document.location.href).searchParams.get(this.key) ?? this.defaultValue;
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

  constructor(private key: string, allowBack: boolean = false, defaultValue: boolean = false) {
    this.#stringState = new QueryParamState(key, allowBack, defaultValue.toString());
  }
}
