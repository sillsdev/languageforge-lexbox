<script lang="ts" module>
  import type {WritingSystemSelection} from '$project/data';

  export type SelectedField = {
    id: string;
    ws: WritingSystemSelection;
  }
</script>

<script lang="ts">
  import {useSemanticDomains, useWritingSystemService} from '$project/data';
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
  placeholder={$t`Any semantic domain`}
  clearable
  idSelector="id" />
