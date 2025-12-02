<script lang="ts">
  import type {Snippet} from 'svelte';
  import {Link} from 'svelte-routing';
  import type {HTMLAnchorAttributes} from 'svelte/elements';

  type Props = HTMLAnchorAttributes & {
    children?: Snippet;
    ref?: HTMLElement | null,
    external?: boolean;
  };

  let {
    href,
    target,
    ref = $bindable(null),
    children,
    external,
    ...rest
  }: Props = $props();
</script>

  {#if target === '_blank' || external || !href}
    <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
    <a bind:this={ref} {href} {target} {...rest}>
      {@render children?.()}
    </a>
  {:else}
    <div bind:this={ref} class="contents">
      <Link to={href} {target} {...rest}>
        {@render children?.()}
      </Link>
    </div>
  {/if}
