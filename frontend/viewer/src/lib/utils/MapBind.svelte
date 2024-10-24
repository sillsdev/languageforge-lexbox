<script lang="ts">

  type IN = $$Generic;
  type OUT = $$Generic;

  let a: IN;
  let b: OUT;

  export {
    a as in,
    b as out,
  };

  export let map: (a: IN) => OUT;
  export let unmap: (b: OUT) => IN;

  $: doMap(a);
  $: doUnmap(b);

  function doMap(_: IN) {
    if (!outOfSync()) return;
    b = map(a);
    lastResult = { a, b };
  }

  function doUnmap(_: OUT) {
    if (!outOfSync()) return;
    a = unmap(b);
    lastResult = { a, b };
  }

  let lastResult: { a: IN, b: OUT } | undefined;
  function outOfSync(): boolean {
    return lastResult?.a !== a || lastResult?.b !== b;
  }
</script>
