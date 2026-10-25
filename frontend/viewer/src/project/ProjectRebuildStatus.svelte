<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import Loading from '$lib/components/Loading.svelte';

  let {projectName}: {projectName: string} = $props();
</script>

<div role="status" aria-live="polite" class="inline-flex flex-col items-center gap-4 m-auto p-4 max-w-md text-center">
  <span class="text-2xl opacity-75">{$t`Updating ${projectName}`}</span>
  <Loading class="size-14 opacity-75"/>
  <p class="text-muted-foreground">
    {$t`This dictionary is being updated for the new version of FieldWorks Lite. It usually takes under a minute, but can take a few minutes for large dictionaries.`}
  </p>
  <p class="font-medium">{$t`Please keep the app open.`}</p>
</div>
