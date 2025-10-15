import { themes } from '@papi/frontend';
import { useData, useDataProvider } from '@papi/frontend/react';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { useMemo, useRef } from 'react';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteMainWindow({ url }: BrowseWebViewOptions) {
  // eslint-disable-next-line no-null/no-null
  const iframe = useRef<HTMLIFrameElement | null>(null);

  // Get the type (light vs dark) of the current theme.
  const themeDataProvider = useDataProvider(themes.dataProviderName);
  const themeData = useData<typeof themes.dataProviderName>(themeDataProvider);
  // eslint-disable-next-line no-type-assertion/no-type-assertion, @typescript-eslint/no-explicit-any
  const [theme] = themeData.CurrentTheme(undefined, {} as any);
  const themeType = useMemo(() => ('type' in theme ? theme.type : undefined), [theme]);

  return url ? (
    <iframe ref={iframe} src={url} style={{ colorScheme: themeType }} title="FieldWorks Lite" />
  ) : (
    <p>Loading...</p>
  );
};
