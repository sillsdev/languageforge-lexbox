<script lang="ts">
  import type {Snippet} from 'svelte';

  import type {Writable} from 'svelte/store';
  import {getContext} from 'svelte';
  import {page} from '$app/state';
  import type {Action} from 'svelte/action';
  import type {ResolvedPathname} from '$app/types';

  interface Props {
    href?: ResolvedPathname;
    children?: Snippet;
  }

  const { href, children }: Props = $props();
  let isCurrentPath = $derived(page.url.pathname === href);

  // TODO: Now that we're in Svelte 5, let's store snippets instead of Elements
  let crumbs: Writable<Element[]> = getContext('breadcrumb-store');

  let setup = false;

  function makeBreadCrumb(element: Element): ReturnType<Action> {
    if (setup) {
      console.error(
        'BreadCrumb already setup, this will mess up the order in which the crumbs are rendered. Probably caused by changing a prop after the component is mounted.',
      );
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
      <!-- eslint-disable-next-line svelte/no-navigation-without-resolve the href is already resolved -->
      <a {href} class="hover:border-b">
        {@render children?.()}
      </a>
    {:else}
      {@render children?.()}
    {/if}
  </span>
</div>
