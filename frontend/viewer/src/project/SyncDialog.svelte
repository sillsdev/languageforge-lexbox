<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import { Duration, DurationUnits } from 'svelte-ux';

  let {
    lbToLocalCount,
    localToLbCount,
    lbToFlexCount,
    flexToLbCount,
    syncLbToLocal,
    syncLbToFlex,
    lastLocalSyncDate,
    lastFlexSyncDate,
  } = $props<{
    lbToLocalCount: number;
    localToLbCount: number;
    lbToFlexCount: number;
    flexToLbCount: number;
    syncLbToLocal: () => void; // Or perhaps Promise<void>
    syncLbToFlex: () => void; // Or perhaps Promise<void>
    lastLocalSyncDate: Date;
    lastFlexSyncDate: Date;
  }>();
</script>

<div class="grid grid-rows-5 grid-cols-3 justify-around rounded-lg border-4">
  <div class="col-span-3 text-center">My computer</div>
  <div class="ml-2">{lbToLocalCount}<Icon icon="i-mdi-arrow-up" /></div>
  <div class="text-center"><Button onclick={syncLbToLocal}><Icon icon="i-mdi-recycle" /></Button></div>
  <div class="mr-2 text-right"><Icon icon="i-mdi-arrow-down" />{localToLbCount}</div>
  <div class="col-span-3 text-center">
    LexBox<br />Last change: <Duration totalUnits={2} start={lastLocalSyncDate} minUnits={DurationUnits.Second} /> ago
  </div>
  <div class="ml-2">{flexToLbCount}<Icon icon="i-mdi-arrow-up" /></div>
  <div class="text-center"><Button onclick={syncLbToFlex}><Icon icon="i-mdi-recycle" /></Button></div>
  <div class="mr-2 text-right"><Icon icon="i-mdi-arrow-down" />{lbToFlexCount}</div>
  <div class="col-span-3 text-center">
    FieldWorks<br />Last change: <Duration totalUnits={2} start={lastFlexSyncDate} minUnits={DurationUnits.Second} /> ago
  </div>
</div>
