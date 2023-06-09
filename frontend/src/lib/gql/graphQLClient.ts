import { CombinedError, mapExchange, type Client, defaultExchanges } from '@urql/svelte';
import { createClient } from '@urql/svelte';
import { browser } from '$app/environment';
import { isObject } from '../util/types';

let globalClient: Client | null = null;
let backendHost = '';
type ServerError = { message: string, code?: string };

function hasError(value: unknown): value is { errors: ServerError[] } {
  if (typeof value !== 'object' || value === null) return false;
  return 'errors' in value && Array.isArray(value.errors);
}

class LexGqlError extends CombinedError {
  constructor(public readonly errors: ServerError[]) {
    super({});
    this.message = errors.map(e => e.message).join(', ');
  }
}

export function createGqlClient(gqlEndpoint?: string): Client {
  const url = `${gqlEndpoint ?? backendHost}/api/graphql`;
  return createClient({
    url,
    exchanges: [
      mapExchange({
        onResult: (result) => {
          if (result.error) return result;
          if (isObject(result.data)) {
            let errors: ServerError[] = [];
            for (const resultValue of Object.values(result.data)) {
              if (hasError(resultValue)) {
                errors = errors.concat(resultValue.errors.map(error => error));
              }
            }
            if (errors.length > 0) {
              result.error = new LexGqlError(errors);
            }
          }
          return result;
        }
      }),
      ...defaultExchanges
    ]
  });
}

export function getClient(gqlEndpoint?: string): Client {
  if (browser) {
    if (globalClient) return globalClient;
    globalClient = createGqlClient('');
    return globalClient;
  } else {
    //We do not cache the client on the server side.
    return createGqlClient(gqlEndpoint);
  }
}

export function storeGqlEndpoint(gqlEndpoint: string): void {
  backendHost = gqlEndpoint;
}

//gqlEndpoint is only required on the server side.
export function initClient(gqlEndpoint?: string): void {
  setClient(createGqlClient(gqlEndpoint));
}

export function setClient(newClient: Client): void {
  globalClient = newClient;
}
