<script lang="ts">
  import type {ComponentProps} from 'svelte';
  import {ViewBase} from '$lib/dotnet-types';
  import type {View} from './view-data';
  // Renaming T prevents the message extractor from falling over due to msg not being a literal string
  import {T as TWithoutExtraction} from 'svelte-i18n-lingui';


  type Props = {
    view: ViewBase | View;
    lite: string;
    classic: string;
  } & Omit<ComponentProps<typeof TWithoutExtraction>, 'msg'>;

  const {
    view,
    lite,
    classic,
    ...rest
  }: Props = $props();

  const viewBase = $derived(typeof view === 'string' ? view : view.base);
  const msg = $derived(viewBase === ViewBase.FwLite ? lite : classic);
</script>

<TWithoutExtraction {msg} {...rest} />
