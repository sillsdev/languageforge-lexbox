// Modified from paranext-core/extensions/src/components/dictionary/back-to-list-button.component.tsx

import type { IEntry } from 'fw-lite-extension';
import { ArrowLeft } from 'lucide-react';
import { ListboxOption, Button, DrawerClose } from 'platform-bible-react';
import { LanguageStrings } from 'platform-bible-utils';

/** Props for the BackToListButton component */
type BackToListButtonProps = {
  /** Callback function to handle back button click, returning to the list view */
  handleBackToListButton?: (option: ListboxOption) => void;
  /** Dictionary entry to display in the button */
  dictionaryEntry: IEntry;
  /** Whether the display is in a drawer or just next to the list */
  isDrawer: boolean;
  /** Localized strings for the button */
  localizedStrings: LanguageStrings;
};

/**
 * A button that appears above the detailed view of a dictionary entry.
 *
 * If the user is viewing the detailed view in a drawer, this button is a drawer close button.
 * Otherwise, it is a regular button.
 *
 * Clicking the button will return the user to the list view of all dictionary entries.
 */
export default function BackToListButton({
  handleBackToListButton,
  dictionaryEntry,
  isDrawer,
  localizedStrings,
}: BackToListButtonProps) {
  if (!handleBackToListButton) return undefined;

  const button = (
    <Button
      onClick={() => handleBackToListButton({ id: dictionaryEntry.id })}
      className="tw-flex tw-items-center"
      variant="link"
    >
      <ArrowLeft className="tw-mr-1 tw-h-4 tw-w-4" />
      {localizedStrings['%fwLiteExtension_dictionary_backToList%']}
    </Button>
  );

  return (
    <div className="tw-mb-4 tw-flex tw-items-center tw-justify-between">
      {isDrawer ? <DrawerClose asChild>{button}</DrawerClose> : button}
    </div>
  );
}
