/**
 * Test Runner for FW Lite E2E Tests
 *
 * This script provides utilities for running E2E tests with different configurations
 * and scenarios. It can be used for local development and CI/CD pipelines.
 */

import { execSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { getTestConfig } from './config';
import { validateTestDataConfiguration } from './helpers/test-data';

interface TestRunOptions {
  scenario?: 'all' | 'smoke' | 'integration' | 'performance';
  browser?: 'chromium' | 'firefox' | 'webkit';
  headed?: boolean;
  debug?: boolean;
  timeout?: number;
  retries?: number;
  workers?: number;
}

/**
 * Main test runner function
 */
export async function runTests(options: TestRunOptions = {}): Promise<void> {
  console.log('🚀 Starting FW Lite E2E Test Runner');

  const config = getTestConfig();

  try {
    // Validate prerequisites
    await validatePrerequisites(config);

    // Build Playwright command
    const command = buildPlaywrightCommand(options);

    console.log('📋 Test Configuration:');
    console.log('   Command:', command);
    console.log('   Options:', options);

    // Execute tests
    console.log('🏃 Executing tests...');
    execSync(command, {
      stdio: 'inherit',
      cwd: process.cwd(),
      env: {
        ...process.env,
        // Pass configuration through environment variables
        TEST_SERVER_HOSTNAME: config.server.hostname,
        TEST_PROJECT_CODE: config.testData.projectCode,
        TEST_USER: config.testData.testUser,
        TEST_DEFAULT_PASSWORD: config.testData.testPassword,
        FW_LITE_BINARY_PATH: config.fwLite.binaryPath,
      }
    });

    console.log('✅ Tests completed successfully');

  } catch (error) {
    console.error('❌ Test execution failed:', error);
    process.exit(1);
  }
}

/**
 * Validate test prerequisites
 */
async function validatePrerequisites(config: any): Promise<void> {
  console.log('🔍 Validating test prerequisites...');

  // Check if FW Lite binary exists
  if (!existsSync(config.fwLite.binaryPath)) {
    throw new Error(`FW Lite binary not found at: ${config.fwLite.binaryPath}`);
  }

  // Validate test data configuration
  validateTestDataConfiguration(config.testData.projectCode);

  // Check if Playwright is installed
  try {
    execSync('npx playwright --version', { stdio: 'pipe' });
  } catch (error) {
    throw new Error('Playwright is not installed. Run: npm install @playwright/test');
  }

  console.log('✅ Prerequisites validated');
}

/**
 * Build Playwright command based on options
 */
function buildPlaywrightCommand(options: TestRunOptions): string {
  const parts = ['npx playwright test'];

  // Add configuration file
  parts.push('--config=frontend/viewer/tests/e2e/playwright.config.ts');

  // Add test pattern based on scenario
  switch (options.scenario) {
    case 'smoke':
      parts.push('--grep="Smoke test"');
      break;
    case 'integration':
      parts.push('--grep="Complete project workflow"');
      break;
    case 'performance':
      parts.push('--grep="Performance"');
      break;
    case 'all':
    default:
      // Run all tests
      break;
  }

  // Add browser selection
  if (options.browser) {
    parts.push(`--project=${options.browser}`);
  }

  // Add headed mode
  if (options.headed) {
    parts.push('--headed');
  }

  // Add debug mode
  if (options.debug) {
    parts.push('--debug');
  }

  // Add timeout
  if (options.timeout) {
    parts.push(`--timeout=${options.timeout}`);
  }

  // Add retries
  if (options.retries !== undefined) {
    parts.push(`--retries=${options.retries}`);
  }

  // Add workers
  if (options.workers) {
    parts.push(`--workers=${options.workers}`);
  }

  return parts.join(' ');
}

/**
 * CLI interface for the test runner
 */
if (require.main === module) {
  const args = process.argv.slice(2);
  const options: TestRunOptions = {};

  // Parse command line arguments
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];

    switch (arg) {
      case '--scenario':
        options.scenario = args[++i] as any;
        break;
      case '--browser':
        options.browser = args[++i] as any;
        break;
      case '--headed':
        options.headed = true;
        break;
      case '--debug':
        options.debug = true;
        break;
      case '--timeout':
        options.timeout = parseInt(args[++i]);
        break;
      case '--retries':
        options.retries = parseInt(args[++i]);
        break;
      case '--workers':
        options.workers = parseInt(args[++i]);
        break;
      case '--help':
        printHelp();
        process.exit(0);
        break;
      default:
        console.warn(`Unknown argument: ${arg}`);
        break;
    }
  }

  runTests(options).catch(error => {
    console.error('Test runner failed:', error);
    process.exit(1);
  });
}

/**
 * Print help information
 */
function printHelp(): void {
  console.log(`
FW Lite E2E Test Runner

Usage: node run-tests.ts [options]

Options:
  --scenario <type>    Test scenario to run (all|smoke|integration|performance)
  --browser <name>     Browser to use (chromium|firefox|webkit)
  --headed            Run tests in headed mode (visible browser)
  --debug             Run tests in debug mode
  --timeout <ms>      Test timeout in milliseconds
  --retries <count>   Number of retries for failed tests
  --workers <count>   Number of parallel workers
  --help              Show this help message

Examples:
  node run-tests.ts --scenario smoke --headed
  node run-tests.ts --scenario integration --browser chromium
  node run-tests.ts --debug --workers 1
  `);
}

export default runTests;
