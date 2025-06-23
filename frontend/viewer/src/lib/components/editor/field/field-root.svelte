<script lang="ts" module>
  import {Context} from 'runed';

  type FieldRootStateProps = {
    labelId: string;
  };

  const fieldRootContext = new Context<FieldRootStateProps>('Field.Root');

  export function usesFieldRoot(props: FieldRootStateProps): FieldRootStateProps {
    return fieldRootContext.set(props);
  }

  type FieldTitleStateProps = FieldRootStateProps;

  export function useFieldTitle(): FieldTitleStateProps {
    return fieldRootContext.get();
  }

  type FieldBodyStateProps = FieldRootStateProps;
  export function tryUseFieldBody(): FieldBodyStateProps | undefined {
    return fieldRootContext.getOr(undefined);
  }
</script>

<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type FieldRootProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  const fieldLabelId = $props.id();
  usesFieldRoot({labelId: fieldLabelId});

  const {
    class: className,
    children,
    ref = $bindable(null),
    ...restProps
  }: FieldRootProps = $props();
</script>

<div class={cn('grid grid-cols-subgrid col-span-full items-baseline', className)} {...restProps}>
  {@render children?.()}
</div>
