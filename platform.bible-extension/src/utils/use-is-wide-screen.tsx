import { useEffect, useState } from 'react';

// Copied from paranext-core/extensions/src/platform-lexical-tools/src/utils/dictionary.utils.ts
export default function useIsWideScreen() {
  const [isWide, setIsWide] = useState(() => window.innerWidth >= 1024);

  useEffect(() => {
    // Matches Tailwind css lg breakpoint
    const mediaQuery = window.matchMedia('(min-width: 1024px)');

    const handler = (e: MediaQueryListEvent) => setIsWide(e.matches);
    mediaQuery.addEventListener('change', handler);

    // Set initial state
    setIsWide(mediaQuery.matches);

    return () => mediaQuery.removeEventListener('change', handler);
  }, []);

  return isWide;
}
