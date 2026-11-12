<script lang="ts">
  import {T, t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';

  const {name, onChange}: {name: string, onChange: () => void} = $props();
</script>

<p class="text-sm text-muted-foreground">
  <T msg="Your changes will show the name #." cmt="# is the user's own name"><span class="font-medium text-foreground wrap-break-word">{name}</span></T>
  <Button variant="link" class="h-auto p-0" onclick={onChange}>{$t`Change`}</Button>
</p>
