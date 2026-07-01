<script lang="ts">
  import {cn} from '$lib/utils.js';

  let {
    value = 0,
    max = 100,
    size = 14,
    strokeWidth = 2,
    class: className,
    trackClass,
    indicatorClass,
  }: {
    value?: number;
    max?: number;
    size?: number;
    strokeWidth?: number;
    class?: string;
    trackClass?: string;
    indicatorClass?: string;
  } = $props();

  const percent = $derived(
    max === 0 ? 0 : Math.min(100, Math.max(0, Math.round((value / max) * 100))),
  );
  const radius = $derived((size - strokeWidth) / 2);
  const circumference = $derived(2 * Math.PI * radius);
  const dashOffset = $derived(circumference - (percent / 100) * circumference);
  const center = $derived(size / 2);
</script>

<div
  class={cn('relative inline-flex shrink-0', className)}
  style:width="{size}px"
  style:height="{size}px"
>
  <svg
    width={size}
    height={size}
    viewBox="0 0 {size} {size}"
    class="absolute inset-0 -rotate-90"
    role="progressbar"
    aria-valuenow={value}
    aria-valuemin={0}
    aria-valuemax={max}
    aria-label="{percent}%"
  >
    <circle
      cx={center}
      cy={center}
      r={radius}
      fill="none"
      class={cn('stroke-current/20', trackClass)}
      stroke-width={strokeWidth}
    />
    <circle
      cx={center}
      cy={center}
      r={radius}
      fill="none"
      class={cn('stroke-current transition-[stroke-dashoffset] duration-300', indicatorClass)}
      stroke-width={strokeWidth}
      stroke-linecap="round"
      stroke-dasharray={circumference}
      stroke-dashoffset={dashOffset}
    />
  </svg>
</div>
