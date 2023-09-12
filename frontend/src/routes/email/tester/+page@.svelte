<script lang="ts">
    import {EmailTemplate, type EmailTemplateProps} from '../emails';
    import {browser} from '$app/environment';
    import {ProjectType, RetentionPolicy} from '$lib/gql/generated/graphql';
    import type {RenderEmailResult} from '$lib/email/emailRenderer.server';

    function absoluteUrl(path: string): string {
      return browser ? `${location.origin}/${path}` : path;
    }

    let emails: Array<EmailTemplateProps & { label?: string }> = [
        {
            template: EmailTemplate.ForgotPassword,
            name: 'Bob',
            resetUrl: absoluteUrl('resetPassword'),
        },
        {
            template: EmailTemplate.VerifyEmailAddress,
            name: 'Bob',
            verifyUrl: absoluteUrl('user?emailResult=verifiedEmail'),
        },
        {
            label: 'Verify New Email Address',
            name: 'Bob',
            template: EmailTemplate.VerifyEmailAddress,
            verifyUrl: absoluteUrl('user?emailResult=changedEmail'),
            newAddress: true,
        },
        {
            label: 'Create Project Request',
            name: 'Admin',
            baseUrl: 'http://localhost:3000',
            template: EmailTemplate.CreateProjectRequest,
            project: {
                id: null,
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
    let emailJson: RenderEmailResult;
    $: if (browser) {
        void fetch('.', {method: 'POST', body: JSON.stringify(currEmail)})
            .then(res => res.json())
            .then(json => {
                if (json.message) {
                    emailJson = {html: json.message, subject: ''};
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
                title={currEmail.template}
        />
    {/if}
</div>
