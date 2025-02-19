import type {ComponentProps, SvelteComponent} from 'svelte';

export function getState<T extends SvelteComponent>(component: T): ComponentProps<T> {
  // eslint-disable-next-line @typescript-eslint/no-unsafe-call
  return (component.$capture_state() as unknown as ComponentProps<T>);
}
