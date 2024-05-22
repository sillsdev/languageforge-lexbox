<script lang="ts">
  import Icon from './Icon.svelte';

  export let pale = false;
  export let color: 'text-error' | undefined = undefined;
</script>

<Icon icon="i-mdi-trash-can" size="text-2xl" {color} {pale} />
