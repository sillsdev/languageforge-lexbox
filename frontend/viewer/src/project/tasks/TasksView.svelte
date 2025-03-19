<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
</script>

<div class="flex flex-col h-full p-4">
  <header class="flex items-center gap-2 mb-4">
    <Icon icon="i-mdi-checkbox-marked" class="size-5" />
    <h1 class="text-xl font-semibold">Tasks</h1>
  </header>

  <div class="flex-1 flex items-center justify-center text-muted-foreground">
    <div class="text-center">
      <Icon icon="i-mdi-checkbox-outline" class="size-12 mx-auto mb-2 opacity-50" />
      <p>No tasks available</p>
    </div>
  </div>
</div>
