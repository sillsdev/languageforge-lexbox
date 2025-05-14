<script lang="ts">
  import type { PageData } from './$types';

  interface Props {
    data: PageData;
  }

  const { data }: Props = $props();

</script>

<h2>Sandbox - Status code: {data.statusCode}</h2>
