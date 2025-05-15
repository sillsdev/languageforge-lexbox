<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';

  let { lbToLocalCount, localToLbCount, lbToFlexCount, flexToLbCount } = $props<{
    lbToLocalCount: number;
    localToLbCount: number;
    lbToFlexCount: number;
    flexToLbCount: number;
  }>();
</script>

<div class="root">
  <div class="local">My computer</div>
  <div class="lb-to-local">{lbToLocalCount}<Icon icon="i-mdi-arrow-up" /></div>
  <div class="lb-to-local-btn"><Button><Icon icon="i-mdi-recycle" /></Button></div>
  <div class="local-to-lb"><Icon icon="i-mdi-arrow-down" />{localToLbCount}</div>
  <div class="lexbox">LexBox<br />Last change: (date)</div>
  <div class="flex-to-lb">{flexToLbCount}<Icon icon="i-mdi-arrow-up" /></div>
  <div class="flex-to-lb-btn"><Button><Icon icon="i-mdi-recycle" /></Button></div>
  <div class="lb-to-flex"><Icon icon="i-mdi-arrow-down" />{lbToFlexCount}</div>
  <div class="fieldworks">FieldWorks<br />Last change: (date)</div>
</div>

<style>
  .root {
    display: grid;
    grid-template:
      'local local local'
      'lb-to-local lb-to-local-btn local-to-lb' minmax(50px, 1fr)
      'lexbox lexbox lexbox'
      'flex-to-lb flex-to-lb-btn lb-to-flex'
      'flex flex flex' / minmax(10px, 0.25fr) 2fr minmax(11px, 0.25fr);
  }

  .local {
    grid-area: local;
    text-align: center;
  }

  .lb-to-local {
    grid-area: lb-to-local;
  }

  .lb-to-local-btn {
    grid-area: lb-to-local-btn;
    text-align: center;
  }

  .local-to-lb {
    grid-area: local-to-lb;
  }

  .lexbox {
    grid-area: lexbox;
    text-align: center;
  }

  .flex-to-lb {
    grid-area: flex-to-lb;
  }
  .lb-to-flex {
    grid-area: lb-to-flex;
  }
  .flex-to-lb-btn {
    grid-area: flex-to-lb-btn;
    text-align: center;
  }
  .fieldworks {
    grid-area: flex;
    text-align: center;
  }
</style>
