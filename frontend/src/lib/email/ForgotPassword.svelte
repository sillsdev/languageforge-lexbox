<script lang="ts">
    import Subject from './Subject.svelte';

    export let name: string = 'hello';
</script>

<Subject value="Hello world!"/>
<mjml>
    <mj-body>
        <mj-section>
            <mj-column>
                <mj-text font-size="32px" color="#F45E43" font-family="helvetica">
                    Hello {name}!
                </mj-text>
                <mj-divider border-color="#F45E43"/>
            </mj-column>
        </mj-section>
    </mj-body>
</mjml> 