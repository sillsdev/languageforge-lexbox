<script lang="ts">
  import type { PageData } from './$types';

  export let data: PageData;
  $: orgs = data.orgs;
</script>

<h1>Organisations</h1>
<ul>
  {#each $orgs as org}
  <li>
    <a
      href="./{org.id}"
    >
      {org.name}
    </a>
  </li>
  {/each}
</ul>

<!--
TODO:

* Sort options: name, created date, # users
* Table view
* Paging
-->
