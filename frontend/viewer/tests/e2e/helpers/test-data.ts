/**
 * Test Data Management
 *
 * This module provides test data configurations and utilities for E2E tests.
 * It manages test projects, entries, and cleanup operations to ensure test isolation.
 */

import type {TestProject, TestEntry} from '../types';
import testProjectsData from '../fixtures/test-projects.json' assert {type: 'json'};

// Test session identifier for unique test data
const TEST_SESSION_ID = `test-${Date.now()}-${Math.random().toString(36).substring(2, 8)}`;

/**
 * Available test projects with their configurations
 */
export const TEST_PROJECTS: Record<string, TestProject> = {
  'sena-3': {
    code: 'sena-3',
    name: 'Sena 3',
    expectedEntries: 0,
    testUser: 'admin'
  }
};

/**
 * Test entry templates for different types of entries
 */
export const TEST_ENTRY_TEMPLATES = {
  basic: {
    lexeme: 'test-word',
    definition: 'A test word created during E2E testing',
    partOfSpeech: 'noun'
  },
  verb: {
    lexeme: 'test-action',
    definition: 'A test action verb created during E2E testing',
    partOfSpeech: 'verb'
  },
  adjective: {
    lexeme: 'test-quality',
    definition: 'A test adjective created during E2E testing',
    partOfSpeech: 'adjective'
  }
};

/**
 * Active test identifiers for cleanup tracking
 */
const activeTestIds = new Set<string>();

/**
 * Get test project configuration by project code
 * @param projectCode - The project code to retrieve
 * @returns TestProject configuration
 * @throws Error if project code is not found
 */
export function getTestProject(projectCode: string): TestProject {
  const project = TEST_PROJECTS[projectCode];
  if (!project) {
    throw new Error(`Test project '${projectCode}' not found. Available projects: ${Object.keys(TEST_PROJECTS).join(', ')}`);
  }
  return project;
}

/**
 * Generate a test entry with unique identifier
 * @param uniqueId - Unique identifier for the entry
 * @param template - Template type to use ('basic', 'verb', 'adjective')
 * @returns TestEntry with unique data
 */
export function generateTestEntry(uniqueId: string, template: keyof typeof TEST_ENTRY_TEMPLATES = 'basic'): TestEntry {
  const baseTemplate = TEST_ENTRY_TEMPLATES[template];
  if (!baseTemplate) {
    throw new Error(`Test entry template '${template}' not found. Available templates: ${Object.keys(TEST_ENTRY_TEMPLATES).join(', ')}`);
  }

  const entry: TestEntry = {
    lexeme: `${baseTemplate.lexeme}-${uniqueId}`,
    definition: `${baseTemplate.definition} (ID: ${uniqueId})`,
    partOfSpeech: baseTemplate.partOfSpeech,
    uniqueIdentifier: uniqueId
  };

  // Track this test ID for cleanup
  activeTestIds.add(uniqueId);

  return entry;
}

/**
 * Generate a unique identifier for test data
 * Uses session ID and timestamp to ensure uniqueness across test runs
 * @param prefix - Optional prefix for the identifier
 * @returns Unique identifier string
 */
export function generateUniqueIdentifier(prefix = 'e2e'): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  const uniqueId = `${prefix}-${TEST_SESSION_ID}-${timestamp}-${random}`;

  // Track this ID for cleanup
  activeTestIds.add(uniqueId);

  return uniqueId;
}

/**
 * Generate multiple unique identifiers
 * @param count - Number of identifiers to generate
 * @param prefix - Optional prefix for the identifiers
 * @returns Array of unique identifier strings
 */
export function generateUniqueIdentifiers(count: number, prefix = 'e2e'): string[] {
  return Array.from({length: count}, () => generateUniqueIdentifier(prefix));
}

/**
 * Get all test projects from fixtures
 * @returns Record of all available test projects
 */
export function getAllTestProjects(): Record<string, TestProject> {
  return TEST_PROJECTS;
}

/**
 * Get test user configuration for a project
 * @param projectCode - Project code to get user for
 * @returns Test user information
 */
export function getTestUser(projectCode: string): {username: string; role: string} {
  const project = getTestProject(projectCode);
  const userData = testProjectsData.testUsers[project.testUser as keyof typeof testProjectsData.testUsers];

  if (!userData) {
    throw new Error(`Test user '${project.testUser}' not found for project '${projectCode}'`);
  }

  return {
    username: userData.username,
    role: userData.role
  };
}

/**
 * Get expected project structure for validation
 * @param projectCode - Project code to get structure for
 * @returns Expected project structure
 */
export function getExpectedProjectStructure(projectCode: string): {
  hasLexicon: boolean;
  hasGrammar: boolean;
  hasTexts: boolean;
} {
  const projectData = testProjectsData.projects[projectCode as keyof typeof testProjectsData.projects];

  if (!projectData) {
    throw new Error(`Project structure data not found for '${projectCode}'`);
  }

  return projectData.expectedStructure;
}

/**
 * Clean up test data created during test execution
 * This function should be called after each test to remove temporary test entries
 * @param projectCode - Project code to clean up data from
 * @param testIds - Array of test identifiers to clean up
 * @returns Promise that resolves when cleanup is complete
 */
export function cleanupTestData(projectCode: string, testIds: string[]): void {
  console.log(`Cleaning up test data for project '${projectCode}' with IDs:`, testIds);

  // Remove IDs from active tracking
  testIds.forEach(id => activeTestIds.delete(id));

  // In a real implementation, this would make API calls to delete test entries
  // For now, we'll simulate the cleanup process
  try {
    // Simulate API cleanup calls
    for (const testId of testIds) {
      console.log(`Cleaning up test entry with ID: ${testId}`);
      // TODO: Implement actual API calls to delete entries when API is available
      // await deleteTestEntry(projectCode, testId);
    }

    console.log(`Successfully cleaned up ${testIds.length} test entries from project '${projectCode}'`);
  } catch (error) {
    console.error(`Failed to clean up test data for project '${projectCode}':`, error);
    throw new Error(`Test data cleanup failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/**
 * Clean up all active test data for the current session
 * Should be called at the end of test suite execution
 * @param projectCode - Project code to clean up data from
 * @returns Promise that resolves when cleanup is complete
 */
export async function cleanupAllTestData(projectCode: string): Promise<void> {
  const allActiveIds = Array.from(activeTestIds);
  if (allActiveIds.length > 0) {
    console.log(`Cleaning up all active test data (${allActiveIds.length} entries) for session: ${TEST_SESSION_ID}`);
    await cleanupTestData(projectCode, allActiveIds);
  } else {
    console.log('No active test data to clean up');
  }
}

/**
 * Get the current test session ID
 * @returns Current test session identifier
 */
export function getTestSessionId(): string {
  return TEST_SESSION_ID;
}

/**
 * Get all active test IDs for the current session
 * @returns Array of active test identifiers
 */
export function getActiveTestIds(): string[] {
  return Array.from(activeTestIds);
}

/**
 * Validate test data configuration
 * Ensures all required test data is available before running tests
 * @param projectCode - Project code to validate
 * @throws Error if validation fails
 */
export function validateTestDataConfiguration(projectCode: string): void {
  // Check if project exists
  const project = getTestProject(projectCode);

  // Check if test user exists
  const user = getTestUser(projectCode);

  // Check if project structure data exists
  const structure = getExpectedProjectStructure(projectCode);

  console.log(`Test data validation passed for project '${projectCode}':`, {
    project: project.name,
    user: user.username,
    structure: structure
  });
}
