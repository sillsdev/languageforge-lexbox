<script lang="ts">
  import Button from './Button.svelte';

  export let loading = false;
  export let form: string | undefined = undefined;
</script>

<!-- https://daisyui.com/components/button -->
<Button type="submit" on:click variant="btn-primary" {loading} {form}>
  <slot />
</Button>
