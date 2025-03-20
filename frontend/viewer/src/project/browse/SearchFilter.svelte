<script lang="ts">
  import {buttonVariants} from '$lib/components/ui/button';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import { Icon } from '$lib/components/ui/icon';
  import { Input } from '$lib/components/ui/input';
  import { t } from 'svelte-i18n-lingui';

  let { search = $bindable() }: { search: string } = $props();
</script>

<Collapsible.Root>
  <div class="relative ml-8">
    <Icon icon="i-mdi-magnify" class="absolute top-1/2 -translate-y-1/2 left-3 size-5" />
    <Input bind:value={search} placeholder={$t`Filter`} class="pl-10 pr-10" />
    <Collapsible.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'absolute top-1/2 -translate-y-1/2 right-1' })}>
      <Icon icon="i-mdi-filter" class="size-5" />
    </Collapsible.Trigger>
  </div>
  <Collapsible.Content>
    more fancy stuff here...
  </Collapsible.Content>
</Collapsible.Root>
