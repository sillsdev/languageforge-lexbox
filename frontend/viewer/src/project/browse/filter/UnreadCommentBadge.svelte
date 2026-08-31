<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {Toggle} from '$lib/components/ui/toggle';
  import * as Tooltip from '$lib/components/ui/tooltip';
  import {resource} from 'runed';
  import {plural} from 'svelte-i18n-lingui';
  import {useProjectContext} from '$project/project-context.svelte';

  const projectContext = useProjectContext();
  const miniLcmApi = $derived(projectContext.maybeApi);

  let {
    unreadComments = $bindable(false)
  }: { unreadComments: boolean } = $props();

  let unreadCountResource = resource(() => miniLcmApi, async (api) => {
    if (!api) return 0;
    return await api.countUnreadComments(undefined);
  }, {
    initialValue: 0,
    debounce: 500
  });
  const unreadCount = $derived(unreadCountResource.current);
  const label = $derived($plural(unreadCount, {one: '# unread comment', other: '# unread comments'}));
</script>

{#if unreadCount > 0}
  <Tooltip.Root>
    <Tooltip.Trigger>
      {#snippet child({props})}
        <Toggle
          {...props}
          bind:pressed={unreadComments}
          variant="outline"
          size="sm"
          aria-label={label}
          class="h-7 min-w-0 gap-1 rounded-full px-2 text-xs text-muted-foreground
                 data-[state=on]:border-primary data-[state=on]:bg-primary/10 data-[state=on]:text-primary
                 data-[state=on]:hover:bg-primary/15 data-[state=on]:hover:text-primary"
        >
          <Icon icon={unreadComments ? 'i-mdi-comment-text' : 'i-mdi-comment-text-outline'} class="size-3.5" />
          {unreadCount}
        </Toggle>
      {/snippet}
    </Tooltip.Trigger>
    <Tooltip.Content>{label}</Tooltip.Content>
  </Tooltip.Root>
{/if}
