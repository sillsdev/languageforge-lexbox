<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import * as Editor from '$lib/components/editor';
  import {writingSystems as demoWritingSystems} from '$project/demo/demo-entry-data';
  import DiffMultiString from '$lib/entry-editor/diff-view/DiffMultiString.svelte';
  import DiffRichText from '$lib/entry-editor/diff-view/DiffRichText.svelte';
  import DiffSelect from '$lib/entry-editor/diff-view/DiffSelect.svelte';
  import DiffMultiSelect from '$lib/entry-editor/diff-view/DiffMultiSelect.svelte';

  const {Story} = defineMeta({
    title: 'editor/diff/overview',
  });

  const ws1 = demoWritingSystems.analysis[0];
  const ws2 = demoWritingSystems.vernacular[0];
  const wss = [ws1, ws2];

  const rich = (text: string) => ({[ws1.wsId]: {spans: [{ws: ws1.wsId, text}]}});
  const domain = (id: string, code: string) => ({id, code});
  const domains = [domain('1', '1.1 Sky'), domain('2', '2.3 Animal'), domain('3', '5.2 Food')];
</script>

<Story name="All diff fields">
  {#snippet template()}
    <Editor.Root>
      <Editor.Grid>
        <div class="col-span-full font-bold">Multi WS — text (char-level diff)</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Unchanged" />
          <Editor.Field.Body subGrid>
            <DiffMultiString before={{[ws1.wsId]: 'Apple'}} after={{[ws1.wsId]: 'Apple'}} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Edited" />
          <Editor.Field.Body subGrid>
            <DiffMultiString before={{[ws1.wsId]: 'Apfel'}} after={{[ws1.wsId]: 'Apfelbaum'}} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Added" />
          <Editor.Field.Body subGrid>
            <DiffMultiString before={{}} after={{[ws1.wsId]: 'New word'}} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Removed" />
          <Editor.Field.Body subGrid>
            <DiffMultiString before={{[ws1.wsId]: 'Old word'}} after={{}} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Multiple writing systems" />
          <Editor.Field.Body subGrid>
            <DiffMultiString
              before={{[ws1.wsId]: 'colour', [ws2.wsId]: 'Farbe'}}
              after={{[ws1.wsId]: 'color', [ws2.wsId]: 'Farbe'}}
              writingSystems={wss} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Complex text edits</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Full replace" />
          <Editor.Field.Body subGrid>
            <DiffMultiString before={{[ws1.wsId]: 'apple pie'}} after={{[ws1.wsId]: 'banana bread'}} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Scattered replacements" />
          <Editor.Field.Body subGrid>
            <DiffMultiString
              before={{[ws1.wsId]: 'a small sweet red fruit that grows on trees'}}
              after={{[ws1.wsId]: 'a small tart green fruit that grows on bushes'}}
              writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Mixed insert / delete / replace" />
          <Editor.Field.Body subGrid>
            <DiffMultiString
              before={{[ws1.wsId]: 'The very large old house on the hill.'}}
              after={{[ws1.wsId]: 'A large, newly-built house high up on the hill!'}}
              writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Rich text</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Edited" />
          <Editor.Field.Body subGrid>
            <DiffRichText before={rich('a small round fruit')} after={rich('a small red round fruit')} writingSystems={[ws1]} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Single select (e.g. part of speech)</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Unchanged" />
          <Editor.Field.Body>
            <DiffSelect before="Noun" after="Noun" />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Changed" />
          <Editor.Field.Body>
            <DiffSelect before="Noun" after="Verb" />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Set" />
          <Editor.Field.Body>
            <DiffSelect before={undefined} after="Noun" />
          </Editor.Field.Body>
        </Editor.Field.Root>
        <Editor.Field.Root>
          <Editor.Field.Title name="Cleared" />
          <Editor.Field.Body>
            <DiffSelect before="Noun" after={undefined} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Multi select (e.g. semantic domains)</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Added & removed" />
          <Editor.Field.Body>
            <DiffMultiSelect
              before={[domains[0], domains[1]]}
              after={[domains[0], domains[2]]}
              idSelector={(d) => d.id}
              labelSelector={(d) => d.code} />
          </Editor.Field.Body>
        </Editor.Field.Root>
      </Editor.Grid>
    </Editor.Root>
  {/snippet}
</Story>
