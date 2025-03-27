<script lang="ts" generics="T extends string | number">
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';
  import InputShell from './input-shell.svelte';
  import {Input} from '.';

  type Props = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {
    ref = $bindable(null),
    inputRef = $bindable(null),
    value = $bindable(),
    class: className,
    placeholder,
    before,
    after,
    ...restProps
  }: Props & {
    value?: T,
    inputRef?: HTMLInputElement | null,
    before?: Snippet,
    after?: Snippet,
  } = $props();
</script>

<InputShell bind:ref class={className} {...restProps}>
  {@render before?.()}
  <Input variant="ghost" {placeholder} class="grow" bind:ref={inputRef} bind:value />
  {@render after?.()}
</InputShell>
