<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import { Icon } from '$lib/components/ui/icon';
  import { defaultEntry } from '$lib/utils';
  import { useWritingSystemService } from '$project/data';
  import type { Snippet } from 'svelte';
  import DevContent from './DevContent.svelte';
  import {useProjectContext} from '$project/project-context.svelte';

  const projectContext = useProjectContext();
  const writingSystems = useWritingSystemService();

  type Props = {
    trigger: Snippet<[{ props: Record<string, unknown> }]>;
  };

  const { trigger }: Props = $props();

  export async function generateEntries(n: number) {
    for (let i = 0; i < n; i++) {
      const entry = defaultEntry();
      const vWsId = writingSystems.defaultVernacular?.wsId;
      if (vWsId) entry.citationForm[vWsId] = `*Test ${Math.random().toString(36).substring(2, 7)}`;
      await projectContext.api.createEntry(entry);
    }
  }
</script>

<DevContent>
  <Dialog.Root>
    <Dialog.Trigger child={trigger} />
    <Dialog.DialogContent>
      <Dialog.DialogHeader>
        <Dialog.DialogTitle>Dev Tools</Dialog.DialogTitle>
      </Dialog.DialogHeader>
      <Button onclick={() => generateEntries(3)}>
        <Icon icon="i-mdi-generator-portable" />
        <span>Generate 3 entries</span>
      </Button>
      <Button onclick={() => generateEntries(10)}>
        <Icon icon="i-mdi-generator-portable" />
        <span>Generate 10 entries</span>
      </Button>
    </Dialog.DialogContent>
  </Dialog.Root>
</DevContent>
