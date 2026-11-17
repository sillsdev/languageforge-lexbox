<script lang="ts">
  // Puts the device in the given release channel and local author name for one story, and puts both back afterwards.
  import {onDestroy, untrack, type Snippet} from 'svelte';
  import {featureFlags} from '$lib/feature-flags/feature-flags.svelte';
  import {useLocalAuthorName} from '$lib/author/local-author-name.svelte';

  const {channel, authorName, children}: {channel?: string, authorName?: string, children: Snippet} = $props();

  const localAuthorName = useLocalAuthorName();
  const before = {channel: featureFlags.channel, authorName: localAuthorName.current};
  untrack(() => {
    if (channel !== undefined) featureFlags.channel = channel;
    if (authorName !== undefined) void localAuthorName.set(authorName);
  });
  onDestroy(() => {
    featureFlags.channel = before.channel;
    void localAuthorName.set(before.authorName);
  });
</script>

{@render children()}
