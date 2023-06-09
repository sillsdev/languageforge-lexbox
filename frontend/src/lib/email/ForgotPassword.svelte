<script lang="ts">
  import Email from '$lib/email/Email.svelte';

  export let name = 'hello';
  export let resetUrl: string;
</script>

<Email subject="Forgot your password?">
  <mj-section>
    <mj-column>
      <mj-text font-size="20px">
        Hello {name}!
      </mj-text>
      <mj-divider border-color="black" />
      <mj-text> To reset your password, click the button below. </mj-text>
      <mj-button href={resetUrl}> Reset password </mj-button>
    </mj-column>
  </mj-section>
</Email>
