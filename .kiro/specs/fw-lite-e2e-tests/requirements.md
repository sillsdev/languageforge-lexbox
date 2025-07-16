# Requirements Document

## Introduction

This feature implements end-to-end testing for FW Lite integration with LexBox to ensure critical functionality like project downloads, data synchronization, and media file handling work correctly. The tests will use Playwright for UI automation and run as part of the CI/CD pipeline to catch regressions before they reach production.

## Requirements

### Requirement 1

**User Story:** As a developer, I want automated end-to-end tests for FW Lite and LexBox integration, so that I can ensure critical workflows continue to work after code changes.

#### Acceptance Criteria

1. WHEN the fw-lite Linux build workflow completes successfully THEN the e2e test workflow SHALL automatically trigger
2. WHEN the e2e test workflow runs THEN it SHALL use the built FW Lite application from the previous workflow
3. WHEN tests run THEN they SHALL be configurable to run against different server environments (local, staging, production)
4. WHEN tests complete THEN they SHALL report pass/fail status and detailed logs for debugging failures

### Requirement 2

**User Story:** As a developer, I want to test the complete project download and modification workflow, so that I can verify data persistence and synchronization work correctly.

#### Acceptance Criteria

1. WHEN the test launches FW Lite THEN it SHALL successfully connect to the configured LexBox server
2. WHEN the test downloads a project (sena-3) THEN it SHALL complete without errors and the project SHALL be available locally
3. WHEN the test creates a new entry via the UI THEN the entry SHALL be saved and visible in the project
4. WHEN the test deletes the local project copy THEN all local files SHALL be removed
5. WHEN the test re-downloads the same project THEN it SHALL retrieve the updated version with the previously created entry
6. WHEN the test searches for the previously created entry THEN it SHALL be found and match the original data

### Requirement 3

**User Story:** As a developer, I want the e2e tests to run in a GitHub Actions workflow, so that they integrate seamlessly with our existing CI/CD pipeline.

#### Acceptance Criteria

1. WHEN the fw-lite build workflow succeeds THEN the e2e test workflow SHALL have access to the built application artifacts
2. WHEN the e2e test workflow runs THEN it SHALL set up the necessary test environment including FW Lite application
3. WHEN tests execute THEN they SHALL use Playwright for UI automation
4. WHEN tests are implemented THEN they SHALL be located in the frontend/viewer folder structure
5. WHEN the workflow completes THEN it SHALL upload test results and screenshots for failed tests

### Requirement 4

**User Story:** As a developer, I want the test framework to handle test data and user expectations, so that tests can run reliably against known project states.

#### Acceptance Criteria

1. WHEN tests run THEN they SHALL expect specific test projects (like sena-3) to be available on the target server
2. WHEN tests run THEN they SHALL expect specific test users with appropriate permissions to be available
3. WHEN tests create test data THEN they SHALL use predictable naming patterns for easy identification and cleanup
4. WHEN tests fail THEN they SHALL provide clear error messages indicating what step failed and why
5. IF test data conflicts exist THEN the test SHALL handle cleanup or use unique identifiers to avoid collisions

### Requirement 5

**User Story:** As a developer, I want comprehensive test coverage for media files and live sync functionality, so that I can prevent regressions in these critical features.

#### Acceptance Criteria

1. WHEN tests run THEN they SHALL verify that media files are properly downloaded and accessible
2. WHEN tests create entries with media attachments THEN the media SHALL be properly synchronized
3. WHEN tests modify existing entries THEN live sync functionality SHALL be verified to work correctly
4. WHEN multiple test scenarios run THEN they SHALL not interfere with each other's data or state
5. WHEN tests complete THEN they SHALL clean up any temporary test data created during execution
