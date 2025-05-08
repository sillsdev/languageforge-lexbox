<script lang="ts">
  import Icon from './Icon.svelte';

  interface Props {
    pale?: boolean;
    color?: 'text-error' | undefined;
  }

  const { pale = false, color = undefined }: Props = $props();
</script>

<Icon icon="i-mdi-trash-can" size="text-2xl" {color} {pale} />
