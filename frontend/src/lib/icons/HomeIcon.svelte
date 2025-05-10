<script lang="ts">
  interface Props {
    size?: 'text-2xl' | 'text-xl';
  }

  const { size = 'text-2xl' }: Props = $props();
</script>

<span class="i-mdi-home-outline {size}"></span>
