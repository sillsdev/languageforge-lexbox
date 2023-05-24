import type {LoadEvent, RequestEvent} from "@sveltejs/kit";
import {CombinedError, mapExchange, type Client, defaultExchanges} from "@urql/svelte";
import {get, writable} from "svelte/store";
import {createClient} from "@urql/svelte";

const clientStore = writable<Client | null>(null);
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

function createGqlClient(event: LoadEvent | RequestEvent, gqlEndpoint?: string) {
    const url = `${gqlEndpoint ?? ''}/api/graphql`;
    return createClient({
        url,
        fetch: event.fetch,
        exchanges: [
            mapExchange({
                onResult: (result) => {
                    if (result.error) return result;
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
