<script lang="ts">
  import {usePublications, useWritingSystemService} from '$project/data';
  import type {IPublication} from '$lib/dotnet-types';
  import {Select} from '$lib/components/field-editors';
  import { t } from 'svelte-i18n-lingui';

  let {value = $bindable()}: { value?: IPublication } = $props();

  const publications = usePublications();
  const writingSystemService = useWritingSystemService();
</script>

<Select
  bind:value
  options={publications.current}
  labelSelector={(pub) => writingSystemService.pickBestAlternative(pub.name, 'analysis')}
  placeholder={$t`Any publication`}
  clearable
  idSelector="id" />
