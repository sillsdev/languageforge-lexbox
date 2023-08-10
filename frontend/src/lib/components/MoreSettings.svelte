<script>
  import t from '$lib/i18n';
</script>

<div class="collapse border-2 border-base-200 collapse-arrow mt-4">
  <input type="checkbox" />
  <div class="collapse-title text-lg">{$t('more_settings.title')}</div>
  <div class="collapse-content">
    <div class="divider mt-0" />
    <div class="flex justify-end">
      <slot />
    </div>
  </div>
</div>
