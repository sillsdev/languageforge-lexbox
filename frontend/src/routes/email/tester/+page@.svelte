<script lang="ts">
    import {EmailTemplate, type EmailTemplateProps} from '../emails';
    import {browser} from '$app/environment';
    import {ProjectType, RetentionPolicy} from '$lib/gql/generated/graphql';
    import type {RenderEmailResult} from '$lib/email/emailRenderer.server';

    function absoluteUrl(path: string): string {
      return `${location.origin}/${path}`;
    }

    let emails: Array<EmailTemplateProps & { label?: string }> = [
        {
            template: EmailTemplate.ForgotPassword,
            name: 'Bob',
            resetUrl: absoluteUrl('resetPassword'),
            lifetime: '3.00:00:00', // 3 days
        },
        {
            template: EmailTemplate.VerifyEmailAddress,
            name: 'Bob',
            verifyUrl: absoluteUrl('user?emailResult=verifiedEmail'),
            lifetime: '3.00:00:00', // 3 days
        },
        {
            label: 'Verify New Email Address',
            name: 'Bob',
            template: EmailTemplate.VerifyEmailAddress,
            verifyUrl: absoluteUrl('user?emailResult=changedEmail'),
            lifetime: '3.00:00:00', // 3 days
            newAddress: true,
        },
        {
            label: 'Create Account Request for Org',
            name: 'Bob',
            template: EmailTemplate.CreateAccountRequestOrg,
            verifyUrl: absoluteUrl('register?name=Bob'), // TODO: Get correct URL
            lifetime: '3.00:00:00', // 3 days
        },
        {
            label: 'Create Account Request for Project',
            name: 'Bob',
            template: EmailTemplate.CreateAccountRequestProject,
            verifyUrl: absoluteUrl('register?name=Bob'), // TODO: Get correct URL
            lifetime: '3.00:00:00', // 3 days
        },
        {
            label: 'Create Project Request',
            name: 'Admin',
            baseUrl: location.origin,
            template: EmailTemplate.CreateProjectRequest,
            project: {
                name: 'My Project',
                code: 'myproj-test-onestory',
                type: ProjectType.OneStoryEditor,
                description: 'My project description',
                retentionPolicy: RetentionPolicy.Test,
                isConfidential: false,
            },
            user: {
                name: 'Bob',
                email: 'test@test.com'
            },
        },
        {
            label: 'Create Project Request (Language Project)',
            name: 'Admin',
            baseUrl: location.origin,
            template: EmailTemplate.CreateProjectRequest,
            project: {
                name: 'My Project',
                code: 'myproj-onestory',
                type: ProjectType.OneStoryEditor,
                description: 'My project description',
                retentionPolicy: RetentionPolicy.Verified,
                isConfidential: true,
            },
            user: {
                name: 'Bob',
                email: 'test@test.com'
            }
        },
        {
            label: 'Create Project Request - custom code',
            name: 'Admin',
            baseUrl: location.origin,
            template: EmailTemplate.CreateProjectRequest,
            project: {
                name: 'My Project',
                code: 'my-proj-custom-onestory',
                type: ProjectType.WeSay,
                description: 'My project description',
                retentionPolicy: RetentionPolicy.Dev,
                projectManagerId: '703701a8-005c-4747-91f2-ac7650455118', // manager from seeding data
                isConfidential: true,
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
