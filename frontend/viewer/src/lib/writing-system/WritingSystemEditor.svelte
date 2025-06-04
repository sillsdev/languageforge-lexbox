<script lang="ts">
    import type {IWritingSystem} from '$lib/dotnet-types';
    import {useMiniLcmApi} from '$lib/services/service-provider';
    import {createEventDispatcher} from 'svelte';
    import {Button} from '$lib/components/ui/button';

    const dispatch = createEventDispatcher<{
        change: { writingSystem: IWritingSystem },
        create: { writingSystem: IWritingSystem }
    }>();
    const miniLcmApi = useMiniLcmApi();
    export let writingSystem: IWritingSystem;
    $: initialWs = JSON.parse(JSON.stringify(writingSystem));
    function updateInitialWs() {
        // eslint-disable-next-line svelte/no-reactive-reassign
        initialWs = JSON.parse(JSON.stringify(writingSystem));
    }
    export let newWs: boolean = false;
    async function onChange() {
        if (newWs) return;
        await miniLcmApi.updateWritingSystem(initialWs, writingSystem);
        updateInitialWs();
        dispatch('change', {writingSystem});
    }

    async function createNew() {
        await miniLcmApi.createWritingSystem(writingSystem.type, writingSystem);
        dispatch('create', {writingSystem});
    }
</script>
<form class="flex flex-col gap-2 p-2">
<!--    <CrdtTextField label="Language Code" readonly={!newWs} on:change={() => onChange()} bind:value={writingSystem.wsId}/>-->
<!--    todo changing the name for FieldWorks writing systems is not yet supported-->
<!--    <CrdtTextField label="Name" readonly={!newWs} on:change={() => onChange()} bind:value={writingSystem.name}/>-->
<!--    <CrdtTextField label="Abbreviation" on:change={() => onChange()} bind:value={writingSystem.abbreviation}/>-->
<!--    <CrdtTextField label="Font" on:change={() => onChange()} bind:value={writingSystem.font}/>-->
    {#if newWs}
        <Button variant="outline" onclick={createNew}>Create new Writing System</Button>
    {:else}
        <span>Changes are saved automatically</span>
    {/if}
</form>
