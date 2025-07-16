/**
 * Global Teardown for FW Lite E2E Tests
 *
 * This file handles global test teardown operations that need to run once
 * after all tests in the suite have completed.
 */

import { getTestConfig } from './config';
import { cleanupAllTestData, getActiveTestIds } from './helpers/test-data';

async function globalTeardown() {
  console.log('🧹 Starting FW Lite E2E Test Suite Global Teardown');

  const config = getTestConfig();

  try {
    // Clean up any remaining test data
    console.log('🗑️  Cleaning up test data...');
    const activeIds = getActiveTestIds();

    if (activeIds.length > 0) {
      console.log(`   Found ${activeIds.length} active test entries to clean up`);
      await cleanupAllTestData(config.testData.projectCode);
      console.log('✅ Test data cleanup completed');
    } else {
      console.log('   No active test data found to clean up');
    }

    // Log test completion summary
    console.log('📊 Test Suite Summary:');
    console.log('   - Project:', config.testData.projectCode);
    console.log('   - Cleaned up entries:', activeIds.length);
    console.log('   - CI Mode:', !!process.env.CI);

    console.log('✅ Global teardown completed successfully');

  } catch (error) {
    console.error('❌ Global teardown failed:', error);
    // Don't throw error in teardown to avoid masking test failures
    console.warn('   Continuing despite teardown errors...');
  }
}

export default globalTeardown;
