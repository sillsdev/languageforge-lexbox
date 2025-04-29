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
    children?: Snippet;
  }

  let { loading = false, form = undefined, variant = 'btn-primary', children }: Props = $props();
</script>

<!-- https://daisyui.com/components/button -->
<Button type="submit" on:click {variant} {loading} {form}>
  {@render children?.()}
</Button>
