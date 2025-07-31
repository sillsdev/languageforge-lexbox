<script lang="ts">
  import {formatDuration} from '$lib/components/ui/format';

  let {
    start,
    end = new Date(),
    smallestUnit = undefined,
    options = {}
  }: { start: Date, end?: Date, options?: Intl.DurationFormatOptions, smallestUnit?: Parameters<typeof formatDuration>[1] } = $props();
  const zero = $derived(formatDuration({seconds: 1}, smallestUnit, options).replace('1', '0'));
</script>
<time>{formatDuration({milliseconds: (end.getTime() - start.getTime())}, smallestUnit, options) || zero}</time>
