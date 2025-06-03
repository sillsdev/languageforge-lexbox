<script lang="ts" generics="T extends string | number">
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';
  import InputShell from './input-shell.svelte';
  import {Input} from '.';

  type Props = WithElementRef<Omit<HTMLAttributes<HTMLDivElement>, 'placeholder'>> & {
    value?: T,
    inputRef?: HTMLInputElement | null,
    before?: Snippet,
    after?: Snippet,
    placeholder?: string | Snippet,
  };

  let {
    ref = $bindable(null),
    inputRef = $bindable(null),
    value = $bindable(),
    class: className,
    placeholder,
    before,
    after,
    ...restProps
  }: Props = $props();

  const stringPlaceholder = $derived(typeof placeholder === 'string' ? placeholder : undefined);
  const snippetPlaceholder = $derived(typeof placeholder === 'function' ? placeholder : undefined);
  const focusRingClass = 'has-[.real-input:focus-visible]:ring-ring has-[.real-input:focus-visible]:outline-none has-[.real-input:focus-visible]:ring-2 has-[.real-input:focus-visible]:ring-offset-2';
</script>

<InputShell bind:ref {focusRingClass} class={className} {...restProps}>
  {@render before?.()}
  <div class="grow flex relative overflow-hidden items-center">
    <Input variant="ghost" placeholder={stringPlaceholder} class="grow real-input" bind:ref={inputRef} bind:value />
    {#if !value && snippetPlaceholder}
      <div class="absolute pointer-events-none text-foreground/50 x-ellipsis whitespace-nowrap">
        {@render snippetPlaceholder()}
      </div>
    {/if}
  </div>
  {@render after?.()}
</InputShell>
