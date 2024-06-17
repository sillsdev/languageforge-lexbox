<script lang="ts">
  import Email from '$lib/email/Email.svelte';
  // import t from '$lib/i18n';

  export let name: string;
</script>

<!-- add $t() -->
<Email subject={'Your project has been approved.'} {name}>
  <mj-text>Your project has been approved.</mj-text>
</Email>
