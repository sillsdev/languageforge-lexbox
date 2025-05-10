<script lang="ts">
  import type { Snippet } from 'svelte';
  import t from '$lib/i18n';

  interface Props {
    column?: boolean;
    children?: Snippet;
  }

  const { column = false, children }: Props = $props();
</script>

<div class="collapse border-2 border-base-200 collapse-arrow mt-4">
  <input type="checkbox" />
  <div class="collapse-title text-lg">{$t('more_settings.title')}</div>
  <div class="collapse-content">
    <div class="divider mt-0"></div>
    <div class="flex justify-end gap-4 max-sm:flex-col" class:flex-col={column} class:sm:items-end={column}>
      {@render children?.()}
    </div>
  </div>
</div>
