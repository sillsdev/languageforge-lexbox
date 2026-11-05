<script lang="ts">
  import * as Avatar from '$lib/components/ui/avatar';
  import {cn} from '$lib/utils';
  import {getAuthorInitials} from './comment-author';
  import {useAuthorColors} from '$lib/activity/author-color-service.svelte';

  const authorColors = useAuthorColors();

  let {
    authorName,
    authorId,
    size = 'md',
    class: className,
  }: {
    authorName?: string;
    authorId?: string;
    size?: 'sm' | 'md';
    class?: string;
  } = $props();

  const initials = $derived(getAuthorInitials(authorName));
  const color = $derived(authorColors.colorFor({authorId, authorName}).avatar);
</script>

<Avatar.Root
  class={cn(size === 'sm' ? 'size-[22px]' : 'size-[26px]', color, className)}
>
  <Avatar.Fallback
    class="bg-transparent text-white"
    style={`font-size: ${size === 'sm' ? '8px' : '10px'}; font-weight: 600`}
  >
    {initials}
  </Avatar.Fallback>
</Avatar.Root>
