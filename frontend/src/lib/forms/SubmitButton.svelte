<script context="module" lang="ts">
  export type SubmitVariant = 'btn-primary' | 'btn-warning' | 'btn-accent' | 'btn-error';
</script>

<script lang="ts">
  import Button from './Button.svelte';

  export let loading = false;
  export let form: string | undefined = undefined;
  export let variant: SubmitVariant = 'btn-primary';
</script>

<!-- https://daisyui.com/components/button -->
<Button type="submit" on:click {variant} {loading} {form}>
  <slot />
</Button>
