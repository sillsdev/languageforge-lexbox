import papi, { logger } from '@papi/frontend';
import { getErrorMessage } from 'platform-bible-utils';

/**
 * Opens the entry that was just added, telling the user when it cannot be opened.
 *
 * The entry is written either way, so this is not a failed add.
 */
export default async function displayAddedEntry(
  projectId: string,
  lexiconCode: string,
  entryId: string,
): Promise<void> {
  try {
    const { success, error } = await papi.commands.sendCommand(
      'lexicon.displayEntry',
      projectId,
      lexiconCode,
      entryId,
    );
    if (!success) throw new Error(error ?? 'Failed to display the new entry');
  } catch (e) {
    logger.error('Error displaying the new entry:', e);
    try {
      await papi.notifications.send({
        message: '%lexicon_error_entryAddedNotShown%',
        severity: 'warning',
      });
    } catch (notifyFailure) {
      logger.warn('Could not report the unshown entry:', getErrorMessage(notifyFailure));
    }
  }
}
