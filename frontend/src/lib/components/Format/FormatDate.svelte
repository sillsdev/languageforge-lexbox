<script lang="ts">
  import { locale } from '$lib/i18n';
  import { formatDate } from './formatters';

  export let value: string | Date | number | null | undefined;
  export let nullLabel: string | undefined = undefined;
  export let options: Intl.DateTimeFormatOptions | undefined = undefined;
</script>

{formatDate(value, $locale, nullLabel, options)}
