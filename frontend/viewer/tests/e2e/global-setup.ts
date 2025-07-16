/**
 * Global Setup for FW Lite E2E Tests
 *
 * This file handles global test setup operations that need to run once
 * before all tests in the suite.
 */

import { getTestConfig } from './config';
import { validateTestDataConfiguration } from './helpers/test-data';

async function globalSetup() {
  console.log('🚀 Starting FW Lite E2E Test Suite Global Setup');

  const config = getTestConfig();

  try {
    // Validate test configuration
    console.log('📋 Validating test configuration...');
    console.log('Test Config:', {
      server: config.server.hostname,
      project: config.testData.projectCode,
      user: config.testData.testUser,
      binaryPath: config.fwLite.binaryPath
    });

    // Validate test data configuration
    console.log('🔍 Validating test data configuration...');
    validateTestDataConfiguration(config.testData.projectCode);

    // Check if FW Lite binary exists
    console.log('🔧 Checking FW Lite binary availability...');
    const fs = await import('node:fs/promises');
    try {
      await fs.access(config.fwLite.binaryPath);
      console.log('✅ FW Lite binary found at:', config.fwLite.binaryPath);
    } catch (error) {
      console.warn('⚠️  FW Lite binary not found at:', config.fwLite.binaryPath);
      console.warn('   Tests will fail if binary is not available during execution');
      console.warn('   Error:', error);
    }

    // Log test environment information
    console.log('🌍 Test Environment Information:');
    console.log('   - Server:', `${config.server.protocol}://${config.server.hostname}`);
    console.log('   - Project:', config.testData.projectCode);
    console.log('   - Test User:', config.testData.testUser);
    console.log('   - Binary Path:', config.fwLite.binaryPath);
    console.log('   - CI Mode:', !!process.env.CI);

    console.log('✅ Global setup completed successfully');

  } catch (error) {
    console.error('❌ Global setup failed:', error);
    throw error;
  }
}

export default globalSetup;
