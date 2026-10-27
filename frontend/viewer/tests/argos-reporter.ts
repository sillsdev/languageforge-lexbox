import ArgosReporter from '@argos-ci/playwright/reporter';
import type {FullResult} from '@playwright/test/reporter';

/**
 * Argos reporter that never fails the run over a rejected upload (e.g. screenshot quota exhausted),
 * but raises a GitHub Actions warning annotation so the failure is still visible on the PR.
 */
export default class WarnOnUploadFailureArgosReporter extends ArgosReporter {
  override async onEnd(result: FullResult) {
    // Without ignoreUploadFailures, Argos logs the error and returns {status: 'failed'}.
    const res = await super.onEnd(result);
    if (res?.status === 'failed') {
      console.log('::warning title=Argos upload failed::Screenshots were not uploaded to Argos, so visual diffs were skipped. See the test step log for the error.');
    }
  }
}
