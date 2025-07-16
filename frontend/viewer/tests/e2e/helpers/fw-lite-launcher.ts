/**
 * FW Lite Application Launcher
 *
 * This module will be implemented in task 2.
 * It manages the FW Lite application lifecycle during tests.
 */

import type { FwLiteManager, LaunchConfig } from '../types';

// Placeholder - to be implemented in task 2
export class FwLiteLauncher implements FwLiteManager {
  async launch(config: LaunchConfig): Promise<void> {
    throw new Error('Not implemented - will be implemented in task 2');
  }

  async shutdown(): Promise<void> {
    throw new Error('Not implemented - will be implemented in task 2');
  }

  isRunning(): boolean {
    throw new Error('Not implemented - will be implemented in task 2');
  }

  getBaseUrl(): string {
    throw new Error('Not implemented - will be implemented in task 2');
  }
}
