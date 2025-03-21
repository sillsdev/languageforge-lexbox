<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import { buttonVariants } from '$lib/components/ui/button';

  let { onDelete } = $props<{
    onDelete?: () => void;
  }>();
</script>

<DropdownMenu.Root>
  <DropdownMenu.Trigger class={buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' })}>
    <Icon icon="i-mdi-dots-vertical" class="cursor-pointer" />
  </DropdownMenu.Trigger>
  <DropdownMenu.Content>
    <DropdownMenu.Item class="cursor-pointer" onSelect={onDelete}>
      <Icon icon="i-mdi-delete" class="mr-2" />
      Delete Entry
    </DropdownMenu.Item>
  </DropdownMenu.Content>
</DropdownMenu.Root>
