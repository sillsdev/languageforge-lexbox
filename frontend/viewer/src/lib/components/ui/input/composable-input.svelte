<script lang="ts" generics="T extends string | number">
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {InputProps} from './input.svelte';
  import type {Snippet} from 'svelte';
  import InputShell from './input-shell.svelte';
  import GhostInput from './ghost-input.svelte';

  type Props = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {
    ref = $bindable(null),
    inputRef = $bindable(null),
    value = $bindable(),
    class: className,
    placeholder,
    inputProps,
    before,
    after,
    ...restProps
  }: Props & {
    value?: T,
    inputProps?: Omit<InputProps, 'ref' | 'value'>,
    inputRef?: HTMLInputElement | null,
    before?: Snippet,
    after?: Snippet,
  } = $props();
</script>

<InputShell bind:ref class={className} {...restProps}>
  {@render before?.()}
  <GhostInput {placeholder} class="grow" {...inputProps} bind:ref={inputRef} bind:value></GhostInput>
  {@render after?.()}
</InputShell>
