<script lang="ts">
  import Subject from '$lib/email/Subject.svelte';
  import t from '$lib/i18n';

  const silLogo = 'https://languageforge.org/Site/views/shared/image/sil_logo.png';
  export let subject: string;
  export let name: string;
</script>

<Subject value={subject} />
<!--https://documentation.mjml.io/#getting-started-->
<mjml>
  <mj-body background-color="white">
    <mj-section>
      <mj-column>
        <mj-text font-size="20px">
          {$t('emails.shared.greeting', { name })}
        </mj-text>
        <mj-divider border-color="black" />
        <slot />
      </mj-column>
    </mj-section>
    <mj-section>
      <mj-column vertical-align="middle">
        <mj-text align="right" font-size="15px"> Language Depot </mj-text>
      </mj-column>
      <mj-column vertical-align="middle" width="50px" padding="0px">
        <mj-image src={silLogo} padding="0px" alt="SIL Logo" height="50px" width="50px" />
      </mj-column>
    </mj-section>
  </mj-body>
</mjml>
