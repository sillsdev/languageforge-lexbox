<script context="module" lang="ts">
  import { NULL_LABEL } from '$lib/i18n';

  const versionLookupTable: Record<number, string> = {
    0: NULL_LABEL,
    7000058: 'FieldWorks 7.2',
    7000059: 'FieldWorks 7.2',
    7000060: 'FieldWorks 7.2',
    7000061: 'FieldWorks 7.2',
    7000062: 'FieldWorks 7.3',
    7000063: 'FieldWorks 7.3',
    7000064: 'FieldWorks 7.3',
    7000065: 'FieldWorks 7.3',
    7000066: 'FieldWorks 8.0',
    7000067: 'FieldWorks 8.0',
    7000068: 'FieldWorks 8.0-8.2',
    7000069: 'FieldWorks 8.2',
    7000070: 'FieldWorks 8.3',
    7000071: 'FieldWorks 8.3',
    7000072: 'FieldWorks 9',
  }
</script>

<script lang="ts">

  export let modelVersion: number;
  export let extraClass = '';
  $: fwModel = versionLookupTable[modelVersion] ?? 'Unknown FieldWorks version';
</script>

<span class={extraClass}>{fwModel}</span>
