<script lang="ts">

  import type {Writable} from 'svelte/store';
  import {getContext} from 'svelte';
  import {page} from '$app/state';
  import type {Action} from 'svelte/action';

  interface Props {
    href?: string | undefined;
    children?: import('svelte').Snippet;
  }

  let { href = undefined, children }: Props = $props();
  let isCurrentPath = $derived(page.url.pathname === href);

  let crumbs: Writable<Element[]> = getContext('breadcrumb-store');

  let setup = false;

  function makeBreadCrumb(element: Element): ReturnType<Action> {
    if (setup) {
      console.error('BreadCrumb already setup, this will mess up the order in which the crumbs are rendered. Probably caused by changing a prop after the component is mounted.');
      return;
    }
    element.remove();
    crumbs.update((crumbs) => [...crumbs, element]);
    setup = true;
    return {
      destroy() {
        crumbs.update((crumbs) => crumbs.filter((crumb) => crumb !== element));
      },
    };
  }
</script>

<div class="hidden">
  <span use:makeBreadCrumb>
    {#if href && !isCurrentPath}
      <a {href} class="hover:border-b">
        {@render children?.()}
      </a>
    {:else}
        {@render children?.()}
    {/if}
  </span>
</div>
