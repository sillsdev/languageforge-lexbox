<script lang="ts">
  import type { PageData } from './$types';

  interface Props {
    data: PageData;
  }

  let { data }: Props = $props();

</script>

<h2>Sandbox - Status code: {data.statusCode}</h2>
