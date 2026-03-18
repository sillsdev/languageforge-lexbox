<script lang="ts">
  import {initViewService, useViewService} from '$lib/views/view-service.svelte';
  import type {TypedViewField, Overrides} from './view-data';
  import type {Snippet} from 'svelte';
  import type {FieldId} from './entity-config';

  interface Props {
    shownFields?: FieldId[];
    respectOrder?: boolean;
    overrides?: Overrides
    children?: Snippet;
  }

  let {
    shownFields = [],
    respectOrder = false,
    children,
    overrides = {}
  }: Props = $props();

  const parentService = useViewService();
  const overrideService = initViewService({persist: false});

  $effect.pre(() => {
    const rootView = parentService.rootView;
    overrideService.overrideView({
      ...rootView,
      entryFields: overrideEntityFields(rootView.entryFields, shownFields, respectOrder),
      senseFields: overrideEntityFields(rootView.senseFields, shownFields, respectOrder),
      exampleFields: overrideEntityFields(rootView.exampleFields, shownFields, respectOrder),
      ...overrides,
    });
  });

  function overrideEntityFields<T extends FieldId>(entityFields: TypedViewField<T>[], shownFields: FieldId[], respectOrder: boolean): TypedViewField<T>[] {
    const filteredFields = entityFields.filter(f => shownFields.includes(f.fieldId));
    if (respectOrder) {
      filteredFields.sort((a, b) => shownFields.indexOf(a.fieldId) - shownFields.indexOf(b.fieldId));
    }
    return filteredFields;
  }
</script>

{@render children?.()}
