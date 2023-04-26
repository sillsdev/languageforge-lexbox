<script lang="ts">
    import Subject from "$lib/email/Subject.svelte";

    const silLogo = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAABkAgMAAAANjH3HAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAMUExURSh2xABcuVaT0fj7/aZ5vGgAAAAJcEhZcwAALiAAAC4gAdUcHhsAAAEQSURBVEjH7dS7DcIwEAZglsgSNIzACEGEMygUGYEpaOjdU4I3oKLKCIxAQZkuSODDzgOi5P6CQJlrIvmTT+c7xyNCMcggg/wsC7MHMmG+EY2NptgcmzJnZkt05pRCfjQlhDJFsnDgF7qy9PKUJIKy9nKXJGHOZydJXNHaf7uyYSYk9o/iKghkcVVfZHEnzWpJ2t2xsijXnFTMRmXb0OQCKVsx7VTc4weUieLW5MkV6eS7U9QdiOLTaVlC3zpR1r5sUdyVu34rqseeCErYrc2UwZ/z2HJlxO+oe1BFQwiJhXKHcoWi6xvSlpyA2C21s1VR/Avlu1PF8F73kSMSZZDERgNZHZDEOyRq931t1OekRC8admBBuTG0EgAAAABJRU5ErkJggg==";
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