<script module lang="ts">
  export type SubmitVariant = 'btn-primary' | 'btn-warning' | 'btn-accent' | 'btn-error';
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  import Button from './Button.svelte';

  interface Props {
    loading?: boolean;
    form?: string;
    variant?: SubmitVariant;
    onclick?: () => void;
    children?: Snippet;
  }

  const { loading = false, form, variant = 'btn-primary', onclick, children }: Props = $props();
</script>

<!-- https://daisyui.com/components/button -->
<Button type="submit" {onclick} {variant} {loading} {form}>
  {@render children?.()}
</Button>
