<script lang="ts" context="module">
  const portals = ['right-toolbar', 'app-bar-menu'] as const;
  type ScottyPortalTarget = typeof portals[number];

  export function initScottyPortalContext(): void {
    portals.forEach((name) => {
      setContext(name, writable<HTMLElement>());
    });
  }

  export function asScottyPortal(elem: HTMLElement, name: ScottyPortalTarget): void {
    const scottyPortal = useScottyPortal(name) as Writable<HTMLElement>;
    scottyPortal.set(elem);
  }

  export function useScottyPortal(name: ScottyPortalTarget): Readable<HTMLElement> {
    const portalStore = getContext<Readable<HTMLElement>>(name);
    // eslint-disable-next-line svelte/require-store-reactive-access
    if (!portalStore) throw new Error(`Portal context not found: ${name as unknown as string}`);
    return portalStore;
  }
</script>

<script lang="ts">
  import {useProjectViewState} from '$lib/services/project-view-state-service';

  import {getContext, setContext} from 'svelte';
  import {portal} from 'svelte-ux';
  import {type Writable, writable, type Readable} from 'svelte/store';

  export let beamMeTo: ScottyPortalTarget;

  $: portalTarget = useScottyPortal(beamMeTo);

  const projectViewState = useProjectViewState();
</script>

<div class="hidden">
  <div class="contents" use:portal={{target: $portalTarget, enabled: !!$portalTarget}}>
    <!-- projectViewState provided for convenience -->
    <slot projectViewState={$projectViewState} />
  </div>
</div>
