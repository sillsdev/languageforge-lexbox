import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import LexiconComboBox from '../components/lexicon-combo-box';

globalThis.webViewComponent = function LexiconSelect({ projectId }: WebViewProps) {
  const [lexicons, setLexicons] = useState<IProjectModel[] | undefined>();

  const selectLexicon = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('lexicon.selectLexicon', projectId ?? '', code);
    },
    [projectId],
  );

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    commands
      .sendCommand('lexicon.lexicons', projectId)
      .then(setLexicons)
      .catch((e) => logger.error('Error fetching lexicons:', JSON.stringify(e)));
  }, [projectId]);

  return <LexiconComboBox lexicons={lexicons} selectLexicon={selectLexicon} />;
};
