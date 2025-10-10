<script lang="ts" module>
  export type SelectedField = {
    id: string;
    ws: WritingSystemSelection;
  }
</script>

<script lang="ts">
  import {useWritingSystemService, type WritingSystemSelection} from '$lib/writing-system-service.svelte';
  import {useSemanticDomains} from '$lib/semantic-domains';
  import type {ISemanticDomain} from '$lib/dotnet-types';
  import {Select} from '$lib/components/field-editors';
  import { t } from 'svelte-i18n-lingui';

  let {value = $bindable()}: { value?: ISemanticDomain } = $props();

  const semanticDomains = useSemanticDomains();
  const writingSystemService = useWritingSystemService();
</script>

<Select
  bind:value
  options={semanticDomains.current}
  labelSelector={(sd) => `${sd.code} ${writingSystemService.pickBestAlternative(sd.name, 'analysis')}`}
  nullOption={$t`Any`}
  idSelector="id" />
