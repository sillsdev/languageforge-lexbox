<script lang="ts">
    import Email from '$lib/email/Email.svelte';
    import t from '$lib/i18n';
    import type {CreateProjectInput} from '$lib/gql/generated/graphql';
    import FormatProjectType from '$lib/components/FormatProjectType.svelte';
    import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';

    export let name: string;
    export let project: CreateProjectInput;
    export let user: { name: string; email: string };

</script>

<Email subject={$t('emails.create_project_request_email.subject', {name: user.name})} {name}>
    <mj-text>{$t('emails.create_project_request_email.heading', {name: user.name})}</mj-text>
    <mj-table>
        <tr>
            <td>{$t('project.create.name')}</td>
            <td>{project.name}</td>
        </tr>
        <tr>
            <td>{$t('project.create.description')}</td>
            <td>{project.description}</td>
        </tr>
        <tr>
            <td>{$t('project.create.code')}</td>
            <td>{project.code}</td>
        </tr>
        <tr>
            <td>{$t('project.create.type')}</td>
            <td><FormatProjectType type={project.type}/></td>
        </tr>
        <tr>
            <td>{$t('project.create.retention_policy')}</td>
            <td><FormatRetentionPolicy policy={project.retentionPolicy}/></td>
        </tr>

    </mj-table>
<!--    todo should have a url here-->
    <mj-button>{$t('project.create.submit')}</mj-button>
</Email>
