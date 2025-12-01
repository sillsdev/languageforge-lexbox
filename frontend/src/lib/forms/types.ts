export type FormEnhance = (el: HTMLFormElement, events?: unknown) => { destroy(): void };

// So that strings that represent errors (e.g. the return type of form submit callbacks)
// can be explicit about what they actually mean.
export type ErrorMessage = string | void;
