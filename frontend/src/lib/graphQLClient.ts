import type {LoadEvent, RequestEvent} from "@sveltejs/kit";
import {CombinedError, mapExchange, type Client, defaultExchanges} from "@urql/svelte";
import {get, writable} from "svelte/store";
import {createClient} from "@urql/svelte";
import { browser } from "$app/environment";

const TRACE_ID_KEY = '__app.trace_id';
const clientStore = writable<Client | null>(null);
type ServerError = { message: string, code?: string };

function hasError(value: unknown): value is { errors: ServerError[] } {
    if (typeof value !== 'object' || value === null) return false;
    return 'errors' in value && Array.isArray(value.errors);
}

class LexGqlError extends CombinedError {
    constructor(errors: CombinedError, traceId?: string)
    constructor(errors: ServerError[], traceId?: string)
    constructor(errors: ServerError[] | CombinedError, public readonly traceId?: string) {
        super(Array.isArray(errors) ? {} : errors);
        if (Array.isArray(errors)) {
            this.message = errors.map(e => e.message).join(', ');
        }
    }
}

function createGqlClient(event: LoadEvent | RequestEvent, gqlEndpoint?: string) {
    const url = `${gqlEndpoint ?? ''}/api/graphql`;
    return createClient({
        url,
      fetch: async (...args) => {
        const response = await event.fetch(...args);
        if (browser) {
          // We only need this for errors that are thrown outside of the context of an active trace.
          // Server errors are always in the scope of the request trace, so: client-only.
          tryPutTraceIdOnResponse(args[1], response);
        }
        return response;
      },
      exchanges: [
          mapExchange({
              onResult: (result) => {
                  if (result.error) {
                      const traceId = tryGetTraceIdFromResponse(result.error.response);
                      result.error = new LexGqlError(result.error, traceId);
                      return result;
                  }
                  if (typeof result.data === 'object') {
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

export function getClient(event?: LoadEvent): Client {
    let client = get(clientStore);
    if (client) {
        return client;
    }
    if (event) {
        client = createGqlClient(event);
        setClient(client);
        return client;
    }
    throw new Error("Client not set");
}

//gqlEndpoint is only required on the server side.
export function initClient(event: LoadEvent | RequestEvent, gqlEndpoint?: string) {
    setClient(createGqlClient(event, gqlEndpoint));
}

export function setClient(newClient: Client) {
    clientStore.set(newClient);
}

function tryPutTraceIdOnResponse(request: RequestInit | undefined, response: Response) {
  const traceparent = (request?.headers as Record<string, string>)?.['traceparent'];
  const traceId = traceparent?.match(/[\d]{2}-([^-]+)/)?.[1]
  if (traceId) {
    (response as object as Record<string, string>)[TRACE_ID_KEY] = traceId;
  }
}

function tryGetTraceIdFromResponse(response?: Response) {
  return (response as object as Record<string, string>)?.[TRACE_ID_KEY];
}
