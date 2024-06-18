<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  import t from '$lib/i18n';

  export let name: string;
</script>

<Email subject={'User Added'} {name}>
  <mj-text></mj-text>
</Email>
