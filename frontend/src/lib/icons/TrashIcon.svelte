<script lang="ts">
  export let pale = false;
  export let color: 'text-error' | undefined = undefined;
</script>

<span class="i-mdi-trash-can text-2xl {color ?? ''}" class:pale />
