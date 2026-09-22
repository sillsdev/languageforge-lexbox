<script lang="ts">
  import * as Avatar from '$lib/components/ui/avatar';
  import {cn} from '$lib/utils';
  import {getAuthorColor, getAuthorInitials} from './comment-author';

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
  const color = $derived(getAuthorColor(authorId, authorName));
</script>

<Avatar.Root
  class={cn(size === 'sm' ? 'size-[22px]' : 'size-[26px]', className)}
  style={`background-color: ${color}`}
>
  <Avatar.Fallback
    class="bg-transparent text-white"
    style={`font-size: ${size === 'sm' ? '8px' : '10px'}; font-weight: 600`}
  >
    {initials}
  </Avatar.Fallback>
</Avatar.Root>
