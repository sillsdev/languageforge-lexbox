import type {enhance} from '$app/forms';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type FormEnhance = (el: HTMLFormElement, events?: any) => ReturnType<typeof enhance>;

// So that strings that represent errors (e.g. the return type of form submit callbacks)
// can be explicit about what they actually mean.
export type ErrorMessage = string | void;
