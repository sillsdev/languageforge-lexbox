<script lang="ts">
  import { LexBoxApi } from '$lib/dotnet/main';
  import { writable } from 'svelte/store';

  let entries = writable<any>([]);
  refresh();

  function setLexeme(value: string, entry: any) {
    LexBoxApi.SetLexeme(entry.id, value);
  }


  function refresh() {
    LexBoxApi.GetEntries().then((result) => {
      entries.set(JSON.parse(result));
      console.log(JSON.parse(result));
    });
  }
</script>

{#each $entries as entry}
  <div class="entry">
    <input type="text" value={entry.lexemeForm.values.en} on:change={() => setLexeme(event.target.value, entry)} />
    <div class="sense">
      {#each entry.senses as sense}
          <p>- {sense.gloss.values.en}: {sense.definition.values.en}</p>
      {/each}
    </div>
  </div>
{/each}

<button class="btn btn-primary" on:click={refresh}>
  Refresh
</button>
