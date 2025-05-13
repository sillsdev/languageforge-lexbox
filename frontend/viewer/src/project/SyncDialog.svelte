<div class="root">
  <p class="local">My computer</p>
  <p class="lb-to-local">↑</p>
  <p class="lb-to-local-btn">Button</p>
  <p class="local-to-lb">↓</p>
  <p class="lexbox">LexBox<br />Last change: (date)</p>
  <p class="flex-to-lb">↑</p>
  <p class="flex-to-lb-btn">Button</p>
  <p class="lb-to-flex">↓</p>
  <p class="fieldworks">FieldWorks<br />Last change: (date)</p>
</div>

<style>
  .root {
    display: grid;
    grid-template:
      'local local local'
      'lb-to-local lb-to-local-btn local-to-lb'
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
  }
  .lb-to-flex-btn {
    grid-area: lb-to-flex-btn;
  }
  .fieldworks {
    grid-area: flex;
    text-align: center;
  }
</style>
