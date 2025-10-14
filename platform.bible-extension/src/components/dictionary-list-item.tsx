// Modified from paranext-core/extensions/src/components/dictionary/dictionary-list-item.component.tsx

import type { DictionaryLanguages, IEntry, ISemanticDomain } from 'fw-lite-extension';
import { cn, Separator } from 'platform-bible-react';
import DomainsDisplay from './domains-display';
import { entryGlossText, entryHeadwordText } from '../utils/entry-display-text';

/** Props for the DictionaryListItem component */
type DictionaryListItemProps = DictionaryLanguages & {
  /** The dictionary entry to display */
  entry: IEntry;
  /** Whether the dictionary entry is selected */
  isSelected: boolean;
  /** Callback function to handle click on the entry */
  onClick: () => void;
  /** Callback function to handle click on a semantic domain */
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
};

/**
 * A list item for a dictionary entry.
 *
 * This component is used to display a dictionary entry in a list of dictionary entries.
 *
 * The component renders a list item with the lemma of the dictionary entry, the number of
 * occurrences in the chapter, and the Strong's codes for the dictionary entry. The component also
 * renders a tooltip that displays the number of occurrences in the chapter.
 *
 * The component uses the `useListbox` hook from the `listbox-keyboard-navigation.util` module to
 * handle keyboard navigation of the list.
 */
export default function DictionaryListItem({
  analysisLanguage,
  entry,
  isSelected,
  onClick,
  onClickSemanticDomain,
  vernacularLanguage,
}: DictionaryListItemProps) {
  return (
    <>
      {/* This component does have keyboard navigation, it is being handled through the useListbox hook */}
      {/* eslint-disable-next-line jsx-a11y/click-events-have-key-events */}
      <li
        role="option"
        aria-selected={isSelected}
        id={entry.id}
        onClick={onClick}
        className={cn(
          'tw-flex tw-flex-col tw-p-2 tw-outline-none focus:tw-ring-2 focus:tw-ring-ring focus:tw-ring-offset-1 focus:tw-ring-offset-background',
          {
            'tw-bg-muted': isSelected,
            'hover:tw-bg-muted': !isSelected,
          },
        )}
        tabIndex={-1}
      >
        <div className="tw-flex tw-items-baseline tw-gap-2">
          <span className="tw-text-sm">{entryHeadwordText(entry, vernacularLanguage)}</span>
        </div>

        <div className="tw-flex tw-items-center tw-gap-2 tw-mt-0.5">
          <p className="tw-text-sm tw-text-muted-foreground tw-truncate">
            {entryGlossText(entry, analysisLanguage)}
          </p>
        </div>

        {onClickSemanticDomain && (
          <div className="tw-flex tw-items-center tw-gap-2 tw-mt-0.5">
            <DomainsDisplay
              analysisLanguage={analysisLanguage}
              domains={entry.senses.flatMap((s) => s.semanticDomains)}
              onClickDomain={onClickSemanticDomain}
            />
          </div>
        )}
      </li>
      <Separator />
    </>
  );
}
