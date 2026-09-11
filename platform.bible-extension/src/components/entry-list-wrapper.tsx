import { useLocalizedStrings } from '@papi/frontend/react';
import { Label } from 'platform-bible-react';
import { ReactNode } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/** Props for the EntryListWrapper component */
type EntryListWrapperProps = {
  elementHeader: ReactNode;
  elementList: ReactNode;
  hasError: boolean;
  isLoading: boolean;
  hasItems: boolean;
};

/** A wrapper layout with a sticky header and a loading/error/no-results/list body. */
export default function EntryListWrapper({
  elementHeader,
  elementList,
  hasError,
  hasItems,
  isLoading,
}: EntryListWrapperProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  // Match className from paranext-core/extensions/src/platform-lexical-tools/src/web-views/dictionary.web-view.tsx
  return (
    <div className="tw:flex tw:flex-col tw:h-[100dvh]">
      <div className="tw:sticky tw:bg-background tw:top-0 tw:z-10 tw:shrink-0 tw:p-2 tw:border-b tw:h-auto">
        {elementHeader}
      </div>

      {isLoading && (
        <div className="tw:flex-1 tw:p-2 tw:space-y-4">
          <Label>{localizedStrings['%lexicon_entryList_loading%']}</Label>
        </div>
      )}
      {hasError && !isLoading && (
        <div className="tw:m-4 tw:flex tw:justify-center">
          <Label>{localizedStrings['%lexicon_entryList_error%']}</Label>
        </div>
      )}
      {!hasItems && !isLoading && !hasError && (
        <div className="tw:m-4 tw:flex tw:justify-center">
          <Label>{localizedStrings['%lexicon_entryList_noResults%']}</Label>
        </div>
      )}
      {hasItems && !hasError && elementList}
    </div>
  );
}
