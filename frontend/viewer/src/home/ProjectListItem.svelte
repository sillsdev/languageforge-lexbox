<script lang="ts">
  import {cn} from '$lib/utils';
  import ProjectTitle from './ProjectTitle.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import type {IProjectModel} from '$lib/dotnet-types';
  import type {ComponentProps} from 'svelte';

  interface Props {
    project?: Pick<IProjectModel, 'name' | 'code'>;
    loading?: boolean;
    skeleton?: boolean;
    subtitle?: string;
  }

  let {
    project = undefined,
    loading = false,
    children = undefined,
    skeleton = false,
    subtitle = undefined,
    ...rest
  }: Props & ComponentProps<ListItem> = $props();
</script>

{#if skeleton}
  <ListItem {...rest}
            class="animate-pulse dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted dark:text-neutral-50/50 cursor-default text-neutral-500">
    <div class="h-4 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-32"></div>
    <div class="h-3 mt-3 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-20"></div>
  </ListItem>
{:else}
  <ListItem
    {...rest}
    class={cn('dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted', loading && 'brightness-50')}>
    {#if subtitle}
      <span><ProjectTitle {project}/></span>
      <span class="text-sm text-muted-foreground">{subtitle}</span>
    {:else}
      <ProjectTitle {project}/>
    {/if}

    {@render children?.()}
  </ListItem>
{/if}
