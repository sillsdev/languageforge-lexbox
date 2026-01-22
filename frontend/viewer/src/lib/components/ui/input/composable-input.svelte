<script lang="ts" generics="T extends string | number">
  import {mergeProps, type WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';
  import InputShell from './input-shell.svelte';
  import {Input} from '.';
  import {cn} from '$lib/utils';
  import type {InputProps} from './input.svelte';

  type Props = WithElementRef<Omit<HTMLAttributes<HTMLDivElement>, 'placeholder'>> & {
    value?: T,
    inputRef?: HTMLInputElement | null,
    before?: Snippet,
    after?: Snippet,
    placeholder?: string | Snippet,
    inputProps?: InputProps,
  };

  let {
    ref = $bindable(null),
    inputRef = $bindable(null),
    value = $bindable(),
    class: className,
    placeholder,
    before,
    after,
    inputProps,
    ...restProps
  }: Props = $props();

  const id = $props.id();

  const stringPlaceholder = $derived(typeof placeholder === 'string' ? placeholder : undefined);
  const snippetPlaceholder = $derived(typeof placeholder === 'function' ? placeholder : undefined);
  const focusRingClass = 'has-[.real-input:focus-visible]:ring-ring has-[.real-input:focus-visible]:outline-none has-[.real-input:focus-visible]:ring-2 has-[.real-input:focus-visible]:ring-offset-2';
</script>

<InputShell bind:ref {focusRingClass} class={cn('gap-0', className)} {...restProps}>
  {@render before?.()}
  <div class="grow flex relative overflow-hidden items-center h-full">
    <Input {id} {...mergeProps(inputProps, { class: 'grow real-input h-full px-2' })} variant="ghost" placeholder={stringPlaceholder} bind:ref={inputRef} bind:value />
    {#if !value && snippetPlaceholder}
      <label for={id} class="absolute pointer-events-none text-foreground/50 x-ellipsis whitespace-nowrap px-2">
        {@render snippetPlaceholder()}
      </label>
    {/if}
  </div>
  {@render after?.()}
</InputShell>
