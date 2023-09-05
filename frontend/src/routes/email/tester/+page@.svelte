<script lang="ts">
    import {EmailTemplate, type EmailTemplateProps} from '../emails';
    import {browser} from '$app/environment';
    import {ProjectType, RetentionPolicy} from '$lib/gql/generated/graphql';
    import type {RenderEmailResult} from '$lib/email/emailRenderer.server';

    let emails: Array<EmailTemplateProps & { label?: string }> = [
        {
            template: EmailTemplate.ForgotPassword,
            name: 'Bob',
            resetUrl: '/resetPassword',
        },
        {
            template: EmailTemplate.VerifyEmailAddress,
            name: 'Bob',
            verifyUrl: '/user?emailResult=verifiedEmail',
        },
        {
            label: 'Verify New Email Address',
            name: 'Bob',
            template: EmailTemplate.VerifyEmailAddress,
            verifyUrl: '/user?emailResult=changedEmail',
            newAddress: true,
        },
        {
            label: 'Create Project Request',
            name: 'Bob',
            template: EmailTemplate.CreateProjectRequest,
            project: {
                name: 'My Project',
                code: 'MYPROJ',
                type: ProjectType.FlEx,
                description: 'My project description',
                retentionPolicy: RetentionPolicy.Dev
            },
            user: {
                name: 'Bob',
                email: 'test@test.com'
            }
        }
    ];

    let currEmail = emails[0];
    let emailJson: RenderEmailResult = null;
    $: if (browser) {
        fetch('.', {method: 'POST', body: JSON.stringify(currEmail)})
            .then(res => res.json())
            .then(json => {
                if (json.message) {
                    emailJson = {html: json.message};
                } else {
                    emailJson = json;
                }
            });
    }


</script>

<label class="label cursor-pointer inline-flex gap-4 m-4">
    <span class="label-text">Email template:</span>
    <select class="select select-info" bind:value={currEmail}>
        {#each emails as email}
            <option value={email}>{email.label ?? email.template.replaceAll(/([a-z])([A-Z])/g, '$1 $2')}</option>
        {/each}
    </select>
</label>

<div class="m-4 mockup-browser border bg-white">
    {#if emailJson}
        <div class="mockup-browser-toolbar text-base-100">
            Subject: {emailJson.subject}
        </div>
        <iframe
                width="100%"
                height="500"
                src={'data:text/html;charset=utf-8,' + encodeURIComponent(emailJson.html)}
                title={currEmail.type}
        />
    {/if}
</div>
