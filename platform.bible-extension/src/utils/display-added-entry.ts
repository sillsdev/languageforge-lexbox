import papi, { logger } from '@papi/frontend';
import { getErrorMessage } from 'platform-bible-utils';

/**
 * Opens the entry that was just added, telling the user when it cannot be opened.
 *
 * The entry is already written, so this failing is not a failed add. It is still reported, because
 * an add whose only visible outcome is a cleared form reads as nothing having happened.
 */
export default async function displayAddedEntry(
  projectId: string,
  lexiconCode: string,
  entryId: string,
): Promise<void> {
  try {
    await papi.commands.sendCommand('lexicon.displayEntry', projectId, lexiconCode, entryId);
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
