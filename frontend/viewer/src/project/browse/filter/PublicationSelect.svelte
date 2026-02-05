<script lang="ts">
  import {usePublications} from '$project/data';
  import type {IPublication} from '$lib/dotnet-types';
  import {Select} from '$lib/components/field-editors';
  import { t } from 'svelte-i18n-lingui';

  let {value = $bindable()}: { value?: IPublication } = $props();

  const publications = usePublications();
</script>

<Select
  bind:value
  options={publications.current}
  labelSelector={(pub) => pub.label}
  placeholder={$t`Any publication`}
  clearable
  idSelector="id" />
