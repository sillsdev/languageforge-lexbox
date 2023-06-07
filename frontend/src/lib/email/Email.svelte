<script lang="ts">
    import Subject from "$lib/email/Subject.svelte";

    const silLogo = "https://languageforge.org/Site/views/shared/image/sil_logo.png";
    export let subject: string;
</script>
<Subject value={subject}/>
<!--https://documentation.mjml.io/#getting-started-->
<mjml>
    <mj-body>
        <slot/>
        <mj-section>
            <mj-column vertical-align="middle">
                <mj-text align="right" font-size="15px">
                    Language Depot
                </mj-text>
            </mj-column>
            <mj-column vertical-align="middle" width="50px" padding="0px">
                <mj-image src={silLogo}  padding="0px" alt="SIL Logo" height="50px" width="50px"/>
            </mj-column>
        </mj-section>
    </mj-body>
</mjml> 