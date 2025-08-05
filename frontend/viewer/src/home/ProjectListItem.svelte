<script lang="ts">
  import ProjectTitle from './ProjectTitle.svelte';
  import ListItem, {type ListItemProps} from '$lib/components/ListItem.svelte';
  import type {IProjectModel} from '$lib/dotnet-types';

  interface Props {
    project?: Pick<IProjectModel, 'name' | 'code'>;
    skeleton?: boolean;
    subtitle?: string;
  }

  let {
    project = undefined,
    children = undefined,
    skeleton = false,
    subtitle = undefined,
    ...rest
    // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  }: Props & ListItemProps = $props();
</script>

{#if skeleton || !project}
  <ListItem {...rest}
            class="animate-pulse dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted dark:text-neutral-50/50 cursor-default text-neutral-500">
    <div class="h-4 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-32"></div>
    <div class="h-3 mt-3 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-20"></div>
  </ListItem>
{:else}
  <ListItem {...rest} class="mb-2">
    {#if subtitle}
      <span><ProjectTitle {project}/></span>
      <span class="text-sm text-muted-foreground">{subtitle}</span>
    {:else}
      <ProjectTitle {project}/>
    {/if}

    {@render children?.()}
  </ListItem>
{/if}
