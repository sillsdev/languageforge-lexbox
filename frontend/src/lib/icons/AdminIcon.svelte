<script lang="ts">
  interface Props {
    size?: 'text-2xl' | 'text-xl';
  }

  let { size = 'text-2xl' }: Props = $props();
</script>

<span class="i-mdi-security {size}"></span>
