
import type {PageLoadEvent} from "./$types";
// stub, just return data loaded on the server since we can't do that client side.
export function load(event: PageLoadEvent) {
    return event.data;
}