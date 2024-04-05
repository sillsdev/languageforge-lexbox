<script lang="ts">
  import 'viewer/component';
  import {type LexboxServiceProvider} from 'viewer/service-provider';
  import {LfClassicLexboxApi} from './lfClassicLexboxApi';


    setLexboxApiProvider();
  // onMount(() => {
  // });

  function setLexboxApiProvider() {
    const serviceProvider: LexboxServiceProvider = window.lexbox.ServiceProvider;
    serviceProvider.setService('LexboxApi', new LfClassicLexboxApi('sena-3'));
  }
</script>
<h1>Hello</h1>
<lexbox-svelte></lexbox-svelte>
