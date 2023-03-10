import type {LoadEvent, RequestEvent} from "@sveltejs/kit";
import type {Client} from "@urql/svelte";
import {get, writable} from "svelte/store";
import {createClient} from "@urql/svelte";

const clientStore = writable<Client | null>(null);

function createGqlClient(event: LoadEvent | RequestEvent) {
    return createClient({
        url: "/api/graphql",
        fetch: event.fetch,
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
