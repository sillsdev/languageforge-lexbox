import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import DictionaryComboBox from '../components/dictionary-combo-box';

globalThis.webViewComponent = function LexiconSelect({ projectId }: WebViewProps) {
  const [dictionaries, setDictionaries] = useState<IProjectModel[] | undefined>();

  const selectDictionary = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('lexicon.selectDictionary', projectId ?? '', code);
    },
    [projectId],
  );

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    commands
      .sendCommand('lexicon.dictionaries', projectId)
      .then(setDictionaries)
      .catch((e) => logger.error('Error fetching dictionaries:', JSON.stringify(e)));
  }, [projectId]);

  return <DictionaryComboBox dictionaries={dictionaries} selectDictionary={selectDictionary} />;
};
