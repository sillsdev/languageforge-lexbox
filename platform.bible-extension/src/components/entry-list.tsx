// Modified from paranext-core/extensions/src/components/dictionary/entry-list.component.tsx

import type { DictionaryLanguages, IEntry, ISemanticDomain } from 'lexicon';
import {
  cn,
  Drawer,
  DrawerContent,
  DrawerTrigger,
  useListbox,
  type ListboxOption,
} from 'platform-bible-react';
import { useState, useEffect, RefObject, useMemo, useRef } from 'react';
import EntryDisplay from './entry-display';
import EntryListItem from './entry-list-item';
import useIsWideScreen from '../utils/use-is-wide-screen';

/** Props for the EntryList component */
type EntryListProps = DictionaryLanguages & {
  /** Array of entries */
  entries: IEntry[];
  /** Callback function to handle character press */
  onCharacterPress?: (char: string) => void;
  /** Callback function to handle click on a semantic domain */
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
};

/**
 * A list of entries.
 *
 * This component renders a listbox of entries. The component also renders a drawer to the right of
 * the list item that contains a detailed view of the entry.
 *
 * The component uses the `useListbox` hook from the `listbox-keyboard-navigation.util` module to
 * handle keyboard navigation of the list.
 */
export default function EntryList({
  analysisLanguage,
  entries,
  onCharacterPress,
  onClickSemanticDomain,
  vernacularLanguage,
}: EntryListProps) {
  const isWideScreen = useIsWideScreen();

  const [selectedEntryId, setSelectedEntryId] = useState<string | undefined>(undefined);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const options: ListboxOption[] = entries.map((entry) => ({ id: entry.id }));

  const selectedEntry = useMemo(() => {
    return entries.find((entry) => entry.id === selectedEntryId);
  }, [entries, selectedEntryId]);

  const clearSelectedEntry = () => {
    setSelectedEntryId(undefined);
  };

  const handleOptionSelect = (option: ListboxOption) => {
    setSelectedEntryId((prevId) => (prevId === option.id ? undefined : option.id));
  };

  const { listboxRef, activeId, handleKeyDown } = useListbox({
    options,
    onOptionSelect: handleOptionSelect,
    onCharacterPress,
  });

  // ref.current expects null and not undefined when we pass it to the div
  // eslint-disable-next-line no-null/no-null
  const entryRef = useRef<HTMLDivElement>(null);

  const scrollToTop = () => {
    entryRef.current?.scrollTo({ top: 0, behavior: 'smooth' });
  };

  useEffect(() => {
    if (selectedEntryId && !isWideScreen) {
      setDrawerOpen(true);
    } else {
      setDrawerOpen(false);
    }
  }, [selectedEntryId, isWideScreen]);

  return (
    <div className="tw-flex tw-flex-row tw-flex-1 tw-overflow-hidden">
      <div
        className={cn('tw-overflow-y-auto tw-px-2 tw-py-2', {
          'tw-w-1/2': isWideScreen && selectedEntryId,
          'tw-w-full': !isWideScreen || !selectedEntryId,
        })}
      >
        <ul
          id="entry-list"
          role="listbox"
          tabIndex={0}
          // The listboxRef is a HTMLElement so that the keyboard navigation can be used with multiple types of elements
          // eslint-disable-next-line no-type-assertion/no-type-assertion
          ref={listboxRef as RefObject<HTMLUListElement>}
          aria-activedescendant={activeId ?? undefined}
          className="tw-outline-none focus:tw-ring-2 focus:tw-ring-ring focus:tw-ring-offset-1 focus:tw-ring-offset-background"
          onKeyDown={handleKeyDown}
        >
          {entries.map((entry) => {
            const isSelected = selectedEntryId === entry.id;
            return (
              <div key={entry.id}>
                <EntryListItem
                  analysisLanguage={analysisLanguage}
                  entry={entry}
                  isSelected={isSelected}
                  onClick={() => setSelectedEntryId(entry.id)}
                  onClickSemanticDomain={onClickSemanticDomain}
                  vernacularLanguage={vernacularLanguage}
                />
              </div>
            );
          })}
        </ul>
      </div>
      {selectedEntryId &&
        selectedEntry &&
        (isWideScreen ? (
          <div ref={entryRef} className="tw-w-1/2 tw-overflow-y-auto tw-p-4">
            <EntryDisplay
              isDrawer={false}
              analysisLanguage={analysisLanguage}
              entry={selectedEntry}
              handleBackToListButton={clearSelectedEntry}
              onClickScrollToTop={scrollToTop}
              onClickSemanticDomain={onClickSemanticDomain}
              vernacularLanguage={vernacularLanguage}
            />
          </div>
        ) : (
          <Drawer
            direction="right"
            open={drawerOpen}
            onOpenChange={() => setSelectedEntryId(undefined)}
          >
            <DrawerTrigger asChild>
              <div />
            </DrawerTrigger>
            <DrawerContent hideDrawerHandle className="tw-max-w-xl">
              <div ref={entryRef} className="tw-overflow-y-auto tw-p-4">
                <EntryDisplay
                  isDrawer
                  analysisLanguage={analysisLanguage}
                  entry={selectedEntry}
                  handleBackToListButton={clearSelectedEntry}
                  onClickScrollToTop={scrollToTop}
                  onClickSemanticDomain={onClickSemanticDomain}
                  vernacularLanguage={vernacularLanguage}
                />
              </div>
            </DrawerContent>
          </Drawer>
        ))}
    </div>
  );
}
