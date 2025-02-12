<script>
  import {mdiArrowLeft} from '@mdi/js';
  import {navigate} from 'svelte-routing';
  import {Button} from 'svelte-ux';
</script>

<Button icon={mdiArrowLeft} on:click={() => navigate('/')} />
