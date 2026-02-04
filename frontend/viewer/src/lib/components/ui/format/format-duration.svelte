<script lang="ts">
  import {formatDuration} from '$lib/components/ui/format';

  type Props = {
    start: Date;
    end?: Date;
    options?: Intl.DurationFormatOptions;
    smallestUnit?: Parameters<typeof formatDuration>[1];
    maxUnits?: number;
  };

  let {start, end = new Date(), smallestUnit = undefined, options = {}, maxUnits = undefined}: Props = $props();
  const zero = $derived(formatDuration({seconds: 1}, smallestUnit, options).replace('1', '0'));
</script>

<time>{formatDuration({milliseconds: end.getTime() - start.getTime()}, smallestUnit, options, maxUnits) || zero}</time>
