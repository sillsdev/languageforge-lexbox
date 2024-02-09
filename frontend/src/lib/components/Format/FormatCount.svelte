<script lang="ts">
  import { locale } from '$lib/i18n';
  import { formatCount } from './formatters';

  export let value: number | bigint | undefined | null;
  export let nullLabel: string | undefined = undefined;
  export let options: Intl.NumberFormatOptions | undefined = undefined;
</script>

{formatCount(value, $locale, nullLabel, options)}
