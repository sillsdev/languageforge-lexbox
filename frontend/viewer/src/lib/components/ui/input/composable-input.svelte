<script lang="ts" generics="T extends string | number">
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';
  import InputShell from './input-shell.svelte';
  import {Input} from '.';
  import {cn} from '$lib/utils';

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

  const focusRingClass = 'has-[.real-input:focus-visible]:ring-ring has-[.real-input:focus-visible]:outline-none has-[.real-input:focus-visible]:ring-2 has-[.real-input:focus-visible]:ring-offset-2';
</script>

<InputShell bind:ref {focusRingClass} class={cn('gap-0', className)} {...restProps}>
  {@render before?.()}
  <Input variant="ghost" {placeholder} class="grow real-input h-full px-2" bind:ref={inputRef} bind:value />
  {@render after?.()}
</InputShell>
