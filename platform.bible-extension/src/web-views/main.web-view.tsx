import { themes } from '@papi/frontend';
import { useData } from '@papi/frontend/react';
import type { BrowseWebViewProps } from 'fw-lite-extension';
import { useMemo, useRef } from 'react';

const DEFAULT_THEME = themes.getCurrentThemeSync();

globalThis.webViewComponent = function FwLiteMainWindow({ url }: BrowseWebViewProps) {
  // eslint-disable-next-line no-null/no-null
  const iframe = useRef<HTMLIFrameElement | null>(null);

  // Get the type (light vs dark) of the current theme.
  const [theme] = useData(themes.dataProviderName).CurrentTheme(undefined, DEFAULT_THEME);
  const themeType = useMemo(() => ('type' in theme ? theme.type : undefined), [theme]);

  return url ? (
    <iframe ref={iframe} src={url} style={{ colorScheme: themeType }} title="FieldWorks Lite" />
  ) : (
    <p>Loading...</p>
  );
};
