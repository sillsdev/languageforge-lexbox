import type {LoadEvent, RequestEvent} from "@sveltejs/kit";
import {CombinedError, mapExchange, type Client, defaultExchanges} from "@urql/svelte";
import {get, writable} from "svelte/store";
import {createClient} from "@urql/svelte";

const clientStore = writable<Client | null>(null);

function hasError(value: unknown): value is { errors: { message: string }[] } {
    if (typeof value !== 'object' || value === null) return false;
    return 'errors' in value && Array.isArray(value.errors);
}

class LexGqlError extends CombinedError {
    constructor(readonly errors: string[]) {
        super({});
        this.message = errors.join(', ');
    }
}

function createGqlClient(event: LoadEvent | RequestEvent) {
    return createClient({
        url: "/api/graphql",
        fetch: event.fetch,
        exchanges: [
            mapExchange({
                onResult: (result) => {
                    if (result.error) return result;
                    if (typeof result.data === 'object') {
                        let errors: string[] = [];
                        for (const resultValue of Object.values(result.data)) {
                            if (hasError(resultValue)) {
                                errors = errors.concat(resultValue.errors.map(error => error.message));
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

export function initClient(event: LoadEvent | RequestEvent) {
    setClient(createGqlClient(event));
}

export function setClient(newClient: Client) {
    clientStore.set(newClient);
}
