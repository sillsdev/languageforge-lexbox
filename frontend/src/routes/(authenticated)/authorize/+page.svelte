<script lang="ts">

  import type {PageData} from './$types';

  export let data: PageData;
</script>

<form method="post" action="/api/login/open-id-auth">

  <p>Do you want to permit "{data.appName}" to access your data? (scopes: {data.scope})</p>
  {#each Object.entries(data.postback) as [key, value]}
    <!--parameters required to resume the auth process-->
    <input type="hidden" name={key} value={value}/>
  {/each}
  <input type="submit" class="btn whitespace-nowrap btn-primary" name="submit.accept" value="Approve"/>
  <input type="submit" class="btn whitespace-nowrap btn-secondary" name="submit.deny" value="Deny"/>
</form>

