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
  <mj-head>
    <mj-style>
      {`
      .break-words {
        word-break: break-word;
      }
      `}
    </mj-style>
  </mj-head>
  <mj-body background-color="white" css-class="break-words">
    <mj-section>
      <mj-column>
        <mj-text font-size="20px">
          {#if name}
            {$t('emails.shared.greeting', { name })}
          {:else}
            {$t('emails.shared.greeting_noname')}
          {/if}
        </mj-text>
        <mj-divider border-color="black" />
        <slot />
      </mj-column>
    </mj-section>
    <mj-section>
      <mj-column>
        <mj-social font-size="15px" icon-size="40px" icon-padding="8px">
          <mj-social-element href="https://languagedepot.org" src={silLogo} alt="SIL Logo">
            {$t('appbar.app_name')}
          </mj-social-element>
        </mj-social>
      </mj-column>
    </mj-section>
  </mj-body>
</mjml>
