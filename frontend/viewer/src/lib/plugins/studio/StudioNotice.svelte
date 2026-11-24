<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import type {IconClass} from '$lib/icon-class';

  interface Props {
    icon: IconClass;
    message: string;
    onBack: () => void;
  }

  const {icon, message, onBack}: Props = $props();
</script>

<div class="h-full grid place-items-center p-4">
  <div class="text-center space-y-3 max-w-md">
    <Icon {icon} class="size-10 text-muted-foreground" />
    <p>{message}</p>
    <Button variant="outline" onclick={onBack}>{$t`Back to plugins`}</Button>
  </div>
</div>
