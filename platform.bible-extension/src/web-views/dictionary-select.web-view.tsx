import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { useCallback, useEffect, useState } from 'react';
import DictionaryComboBox from '../components/dictionary-combo-box';

globalThis.webViewComponent = function FwDictionarySelect({ projectId }: WebViewProps) {
  const [fwDictionaries, setFwDictionaries] = useState<IProjectModel[] | undefined>();

  const selectDictionary = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('fwLiteExtension.selectDictionary', projectId ?? '', code);
    },
    [projectId],
  );

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    commands
      .sendCommand('fwLiteExtension.fwDictionaries', projectId)
      .then(setFwDictionaries)
      .catch((e) => logger.error('Error fetching FieldWorks dictionaries:', JSON.stringify(e)));
  }, [projectId]);

  return <DictionaryComboBox dictionaries={fwDictionaries} selectDictionary={selectDictionary} />;
};
