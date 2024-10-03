<script lang="ts">
  import { HeaderPage, HomeBreadcrumb } from '$lib/layout';
  import t from '$lib/i18n';
</script>

<HomeBreadcrumb />
<HeaderPage wide title={$t('about.title')}>
  <div class="prose">
    <p>Lexbox is a SIL Lexical Tools project.</p>
    <p>
      We host project data for
      <a href="https://software.sil.org/fieldworks/">FieldWorks</a>,
      <a href="https://languageforge.org/">Language Forge</a>, and
      <a href="https://software.sil.org/onestoryeditor/">OneStory Editor</a>.
      It is provided as a free service to language communities by
      <a href="https://software.sil.org/">SIL Language Technology</a> and
      <a href="https://li.payap.ac.th/">Linguistics Institute at Payap University</a>
      in Chiang Mai, Thailand.
    </p>
    <p>
      Lexbox is a free and open source project, and is licensed under the
      <a href="https://github.com/sillsdev/languageforge-lexbox/blob/main/LICENSE">MIT license</a>.
    </p>
  </div>
</HeaderPage>
