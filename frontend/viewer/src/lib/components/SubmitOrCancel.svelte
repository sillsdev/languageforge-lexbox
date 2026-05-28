<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {cn} from '$lib/utils';
  import {watch} from 'runed';
  import {t} from 'svelte-i18n-lingui';

  interface Props {
    canSubmit?: boolean;
    onSubmit: () => void;
    onCancel: () => void;
    submitLabel?: string;
    cancelLabel?: string;
    class?: string;
  }

  let {
    canSubmit = true,
    onSubmit,
    onCancel,
    submitLabel = $t`Submit`,
    cancelLabel = $t`Cancel`,
    class: className,
  }: Props = $props();

  // svelte-ignore state_referenced_locally
  let wasSubmittable = $state(canSubmit);

  watch(
    () => canSubmit,
    (canSubmit) => {
      wasSubmittable = canSubmit || wasSubmittable;
    },
  );

  function handleCancel() {
    wasSubmittable = false;
    onCancel?.();
  }

  function handleSubmit() {
    wasSubmittable = false;
    onSubmit?.();
  }
</script>

<div class={cn('flex gap-4 items-stretch md:justify-end flex-nowrap w-full self-stretch', className)}>
  <Button
    variant="secondary"
    class={cn('transition-all min-w-fit max-md:basis-0', canSubmit || wasSubmittable ? 'max-md:grow' : 'max-md:grow-3')}
    onclick={handleCancel}
  >
    {cancelLabel}
  </Button>
  <Button
    variant="default"
    disabled={!canSubmit}
    class={cn('transition-all min-w-fit max-md:basis-0', canSubmit || wasSubmittable ? 'max-md:grow-3' : 'max-md:grow')}
    onclick={handleSubmit}
  >
    {submitLabel}
  </Button>
</div>
