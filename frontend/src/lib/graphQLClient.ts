import {Client} from "@urql/svelte";
import {writable} from "svelte/store";

export const client = writable<Client | null>(null);
