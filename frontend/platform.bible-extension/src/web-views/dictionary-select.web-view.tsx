import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { useCallback, useEffect, useState } from 'react';
import DictionaryComboBox from '../components/dictionary-combo-box';

globalThis.webViewComponent = function fwDictionarySelect(props: WebViewProps) {
  const [fwDictionaries, setFwDictionaries] = useState<IProjectModel[] | undefined>();

  const selectDictionary = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('fwLiteExtension.selectDictionary', props.projectId!, code);
    },
    [props.projectId],
  );

  useEffect(() => {
    logger.info(`This web view was opened for project '${props.projectId}'`);
    void commands
      .sendCommand('fwLiteExtension.fwDictionaries')
      .then(setFwDictionaries)
      .catch((e) => void logger.error('Error fetching FieldWorks dictionaries:', e));
  }, [props.projectId]);

  return <DictionaryComboBox dictionaries={fwDictionaries} selectDictionary={selectDictionary} />;
};
