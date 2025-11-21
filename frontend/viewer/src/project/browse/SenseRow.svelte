<script lang="ts">
  import ListItem, {type ListItemProps} from '$lib/components/ListItem.svelte';
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type {ISense} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$project/data';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import type {Snippet} from 'svelte';

  interface Props extends WithoutChildrenOrChild<ListItemProps> {
    sense: ISense;
    badge?: Snippet;
  }

  const {
    sense,
    badge,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
</script>

<ListItem {...rest}>
  <div class="flex justify-between items-center">
    <p class="font-medium text-xl">{writingSystemService.firstGloss(sense).padStart(1, '–')}</p>
    {@render badge?.()}
  </div>
  <div class="flex justify-between items-baseline">
    <p class="text-muted-foreground">{writingSystemService.firstDef(sense).padStart(1, '–')}</p>
    {#if sense.partOfSpeech}
      <Badge variant="default" class="bg-primary/60">
        {writingSystemService.pickBestAlternative(sense.partOfSpeech.name, 'analysis')}
      </Badge>
    {/if}
  </div>
</ListItem>
