# FW Lite E2E Tests

This directory contains end-to-end tests for FW Lite integration with LexBox.

## Directory Structure

```
e2e/
├── README.md                 # This file
├── tsconfig.json            # TypeScript configuration for E2E tests
├── types.ts                 # TypeScript type definitions
├── config.ts                # Test configuration and constants
├── helpers/                 # Test helper utilities
│   ├── fw-lite-launcher.ts  # FW Lite application management
│   ├── project-operations.ts # Project download/management helpers
│   └── test-data.ts         # Test data utilities
├── fixtures/                # Test data and fixtures
│   └── test-projects.json   # Expected test project configurations
└── *.test.ts               # Test files
```

## Configuration

Tests can be configured through environment variables:

- `TEST_SERVER_HOSTNAME`: Target server hostname (default: localhost:5137)
- `FW_LITE_BINARY_PATH`: Path to FW Lite binary (default: ./fw-lite-linux/linux-x64/FwLiteWeb)
- `TEST_PROJECT_CODE`: Test project code (default: sena-3)
- `TEST_USER`: Test user (default: admin)
- `TEST_DEFAULT_PASSWORD`: Test user password (default: pass)

## Usage

E2E tests are designed to run as part of the CI/CD pipeline after successful FW Lite builds.

For local development, ensure:
1. FW Lite binary is available at the configured path
2. Target server is running and accessible
3. Test projects and users are available on the server

## Test Data

Tests expect specific test projects (like 'sena-3') and users to be available on the target server. See `fixtures/test-projects.json` for expected configurations.
