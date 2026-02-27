<script lang="ts" module>
  import {Context} from 'runed';
  import type {FieldId} from '$lib/views/fields';
  const fieldIdSymbol = Symbol('fw-lite-field-id');
  class FieldRootState {
    //require using the constructor when this type is used
    private readonly [fieldIdSymbol] = true;
    labelId: string;
    fieldId? = $state<FieldId>();
    label? = $state<string>();

    constructor(labelId: string) {
      this.labelId = labelId;
    }
  }

  const fieldRootContext = new Context<FieldRootState>('Field.Root');

  export function usesFieldRoot(props: FieldRootState): FieldRootState {
    return fieldRootContext.set(props);
  }

  type FieldTitleStateProps = FieldRootState;

  export function useFieldTitle(): FieldTitleStateProps {
    return fieldRootContext.get();
  }

  type FieldBodyStateProps = FieldRootState;
  export function tryUseFieldBody(): FieldBodyStateProps | undefined {
    return fieldRootContext.getOr(undefined);
  }
</script>

<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type FieldRootProps = {fieldId?: FieldId} & WithElementRef<HTMLAttributes<HTMLDivElement>>;

  const fieldLabelId = $props.id();
  const fieldProps = usesFieldRoot(new FieldRootState(fieldLabelId));
  $effect(() => {
    fieldProps.fieldId = fieldId;
  });

  const {
    class: className,
    children,
    fieldId = undefined,
    ref = $bindable(null),
    ...restProps
  }: FieldRootProps = $props();
</script>

<div
  style="grid-area: {fieldId}"
  class={cn('grid grid-cols-subgrid col-span-full items-baseline', className)}
  {...restProps}
>
  {@render children?.()}
</div>
