import { derived, type Readable } from "svelte/store";
import type { IEntry, WritingSystems } from "./mini-lcm";

export type Initializable<T> = {
  initialized: false;
} | {
  initialized: true;
  value: T;
}

export type AppEntries = Readable<IEntry[]>;
export type AppWritingSystems = Readable<WritingSystems>;

export function deriveInitializedValue<T>(value: Readable<Initializable<T>>): Readable<T> {
  return derived(value, (value, set) => {
    if (value.initialized) set(value.value);
  });
}
