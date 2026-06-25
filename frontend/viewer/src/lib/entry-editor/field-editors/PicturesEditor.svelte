<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {Button} from '$lib/components/ui/button';
  import PictureCarousel from './PictureCarousel.svelte';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    value: IPicture[];
    readonly?: boolean;
  };
  const {value, readonly = false}: Props = $props();
</script>

{#if value.length > 0}
  <PictureCarousel pictures={value} />
{:else if !readonly}
  <!-- TODO: wire this up to actually add a picture once a create-picture API is exposed
       to the frontend. MiniLcmJsInvokable currently has no create/update picture method,
       so the button mirrors "+ Component" but is disabled for now. -->
  <Button icon="i-mdi-plus" size="xs" disabled>
    {$t`Picture`}
  </Button>
{:else}
  <div class="text-muted-foreground p-1">
    {$t`No pictures`}
  </div>
{/if}
