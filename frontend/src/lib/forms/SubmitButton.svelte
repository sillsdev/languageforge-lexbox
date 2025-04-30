<script module lang="ts">
  export type SubmitVariant = 'btn-primary' | 'btn-warning' | 'btn-accent' | 'btn-error';
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  import Button from './Button.svelte';

  interface Props {
    loading?: boolean;
    form?: string | undefined;
    variant?: SubmitVariant;
    onclick: () => void;
    children?: Snippet;
  }

  let { loading = false, form = undefined, variant = 'btn-primary', onclick, children }: Props = $props();
</script>

<!-- https://daisyui.com/components/button -->
<Button type="submit" {onclick} {variant} {loading} {form}>
  {@render children?.()}
</Button>
