<script lang="ts">
  import type {ComponentProps} from 'svelte';
  import type {View, ViewType} from './view-data';
  // Renaming T prevents the message extractor from falling over due to msg not being a literal string
  import {T as TWithoutExtraction} from 'svelte-i18n-lingui';


  type Props = {
    view: ViewType | View;
    lite: string;
    classic: string;
  } & Omit<ComponentProps<typeof TWithoutExtraction>, 'msg'>;

  const {
    view,
    lite,
    classic,
    ...rest
  }: Props = $props();

  const viewType = $derived(typeof view === 'string' ? view : view.type);
  const msg = $derived(viewType === 'fw-lite' ? lite : classic);
</script>

<TWithoutExtraction {msg} {...rest} />
