<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { Input } from '$lib/components/ui/input';
  import { t } from 'svelte-i18n-lingui';

  let {search = $bindable()}: {search: string} = $props();
</script>

<div class="mb-4 p-2 pr-4 relative">
  <Icon icon="i-mdi-magnify" class="absolute top-1/2 -translate-y-1/2 left-5 size-5" />
  <Input
    bind:value={search}
    placeholder={$t`Filter`}
    class="pl-12 pr-10"
  />
  <Icon icon="i-mdi-filter" class="absolute top-1/2 -translate-y-1/2 right-6 size-5" />
</div>
