<script>
  import t from '$lib/i18n';

  import silLogo from '$lib/assets/sil_logo_glyph.png';
  import payapLogo from '$lib/assets/payap-logo.png';
</script>

<footer class="bg-neutral text-neutral-content py-2 px-3 md:py-4 md:px-8 max-md:text-sm">
  <div class="flex justify-between items-center m-auto">
    <div class="space-y-2">
      <div class="flex gap-4 items-center">
        <a href="https://www.sil.org/">
          <!-- https://www.sil.org/about/media -->
          <img class="h-6 md:h-10" height="48" src={silLogo} alt="SIL Global logo" />
        </a>
        <a href="https://li.payap.ac.th/">
          <!-- http://info.payap.ac.th/info/link_download/ -->
          <img class="h-6 md:h-9" height="44" src={payapLogo} alt="Payap University logo" />
        </a>
      </div>
      <div>
        © {new Date().getFullYear()}
        <a class="hover:link" href="https://www.sil.org/">SIL Global</a>
      </div>
    </div>
    <div class="grid grid-cols-2 gap-x-4 text-right">
      <a class="hover:link" href="/about">{$t('about.title')}</a>
      <a class="hover:link" href="https://software.sil.org/language-software-terms-of-use/">{$t('footer.terms_of_use')}</a>
      <a class="hover:link" href="mailto:admin@languagedepot.org" target="_blank">{$t('footer.contact')}</a>
      <a class="hover:link" href="https://software.sil.org/language-software-privacy-policy/">{$t('footer.privacy_policy')}</a>
    </div>
  </div>
</footer>
