<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';

  const {name, onChange}: {name: string, onChange: () => void} = $props();
</script>

<p class="text-sm text-muted-foreground">
  {$t`Your changes will show the name ${name}.`}
  <Button variant="link" class="h-auto p-0" onclick={onChange}>{$t`Change`}</Button>
</p>
