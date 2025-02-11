<script lang="ts" context="module">
  import {getContext, setContext} from 'svelte';
  import {type Writable, writable, type Readable, get} from 'svelte/store';
  import type {ActionReturn} from 'svelte/action';

  const name = 'scotty-portal-context' as const;
  const portals = ['right-toolbar', 'app-bar-menu'] as const;
  type ScottyPortalTarget = typeof portals[number];
  type ScottyPortalContext = Record<ScottyPortalTarget, Readable<HTMLElement | undefined>>;

  export function initScottyPortalContext(): void {
    const portalStoreMap: ScottyPortalContext = {
      'right-toolbar': writable<HTMLElement | undefined>(),
      'app-bar-menu': writable<HTMLElement | undefined>(),
    };
    setContext(name, portalStoreMap);
  }

  export function asScottyPortal(elem: HTMLElement, name: ScottyPortalTarget): ActionReturn {
    const portalStoreMap = useScottyPortals();
    const portalStore = portalStoreMap[name] as Writable<HTMLElement | undefined>;
    portalStore.set(elem);
    return {
      destroy() {
        if (get(portalStore) === elem) portalStore.set(undefined);
      }
    };
  }

  export function useScottyPortals(): ScottyPortalContext {
    const portalStoreMap = getContext<ScottyPortalContext>(name);
    // eslint-disable-next-line svelte/require-store-reactive-access
    if (!portalStoreMap) throw new Error(`Portal context not found: ${name as unknown as string}`);
    return portalStoreMap;
  }
</script>

<script lang="ts">
  import {useProjectViewState} from '$lib/views/project-view-state-service';
  import {portal} from 'svelte-ux';

  export let beamMeTo: ScottyPortalTarget;
  const scottyPortals = useScottyPortals();
  $: portalTarget = scottyPortals[beamMeTo];

  const projectViewState = useProjectViewState();
</script>

<div class="hidden">
  <div class="contents" use:portal={{target: $portalTarget, enabled: !!$portalTarget}}>
    <!-- projectViewState provided for convenience -->
    <slot projectViewState={$projectViewState} />
  </div>
</div>
