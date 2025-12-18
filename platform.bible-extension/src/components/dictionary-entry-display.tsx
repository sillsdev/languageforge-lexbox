// Modified from paranext-core/extensions/src/components/dictionary/dictionary-entry-display.component.tsx

import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryLanguages, IEntry, ISemanticDomain } from 'fw-lite-extension';
import { ChevronUpIcon } from 'lucide-react';
import { Button, DrawerDescription, DrawerTitle, Separator } from 'platform-bible-react';
import BackToListButton from './back-to-list-button';
import DomainsDisplay from './domains-display';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import {
  entryGlossText,
  entryHeadwordText,
  partOfSpeechText,
  senseDefinitionText,
  senseGlossText,
} from '../utils/entry-display-text';

/** Props for the DictionaryEntryDisplay component */
export type DictionaryEntryDisplayProps = DictionaryLanguages & {
  /** Dictionary entry object to display */
  dictionaryEntry: IEntry;
  /** Whether the display is in a drawer or just next to the list */
  isDrawer: boolean;
  /** Callback function to handle back button click, returning to the list view */
  handleBackToListButton?: () => void;
  /** Callback function to handle scroll to top */
  onClickScrollToTop: () => void;
  /** Callback function to handle click on a semantic domain */
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
};

/**
 * Renders a detailed view of a dictionary entry, displaying its key properties such as Hebrew text,
 * transliteration, Strong's number, part of speech, definition, and usage occurrences. Includes a
 * back button to navigate back to the list view.
 */
export default function DictionaryEntryDisplay({
  analysisLanguage,
  dictionaryEntry,
  isDrawer,
  handleBackToListButton,
  onClickScrollToTop,
  onClickSemanticDomain,
  vernacularLanguage,
}: DictionaryEntryDisplayProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  // Cannot use Drawer components when there is no Drawer, if the screen is considered wide it will render Button and span here.
  const TitleComponent = isDrawer ? DrawerTitle : 'span';
  const DescriptionComponent = isDrawer ? DrawerDescription : 'span';

  /** If domain is clicked, call the provided callback then close the drawer. */
  const onClickDomain = onClickSemanticDomain
    ? (domain: ISemanticDomain) => {
        onClickSemanticDomain(domain);
        if (isDrawer) {
          handleBackToListButton?.();
        }
      }
    : undefined;

  return (
    <>
      <BackToListButton
        isDrawer={isDrawer}
        localizedStrings={localizedStrings}
        handleBackToListButton={handleBackToListButton}
      />
      <div className="tw-mb-4">
        <div className="tw-flex tw-items-baseline tw-justify-between tw-gap-2">
          <span className="tw-flex tw-flex-row tw-items-baseline tw-gap-2">
            <TitleComponent className="tw-text-2xl tw-font-bold">
              {entryHeadwordText(dictionaryEntry, vernacularLanguage)}
            </TitleComponent>
            <DescriptionComponent className="tw-text-lg tw-text-muted-foreground">
              {entryGlossText(dictionaryEntry, analysisLanguage)}
            </DescriptionComponent>
          </span>
        </div>
      </div>

      <Separator className="tw-my-3" />

      <div className="tw-mb-4">
        <h3 className="tw-mb-1 tw-font-semibold">
          {localizedStrings['%fwLiteExtension_entryDisplay_senses%']}
        </h3>

        <div className="tw-flex tw-flex-col tw-gap-3">
          {dictionaryEntry.senses.filter(Boolean).map((sense, senseIndex) => (
            <div
              key={sense.id}
              className="tw-flex tw-w-full tw-h-fit tw-flex-col tw-items-start tw-border tw-rounded-lg tw-shadow-sm tw-p-4 tw-transition-colors"
            >
              <div className="tw-flex tw-items-baseline tw-gap-2">
                <span className="tw-font-bold tw-text-accent-foreground">{senseIndex + 1}</span>
                <span className="tw-text-base">{senseGlossText(sense, analysisLanguage)}</span>
              </div>

              {Object.values(sense.definition).some(Boolean) && (
                <div className="tw-mt-1 tw-max-w-lg tw-text-start tw-text-sm tw-text-muted-foreground">
                  <span>{senseDefinitionText(sense, analysisLanguage)}</span>
                </div>
              )}

              {sense.partOfSpeech?.id && (
                <div className="tw-mt-1 tw-max-w-lg tw-text-start tw-text-sm tw-text-muted-foreground">
                  <span>{`${localizedStrings['%fwLiteExtension_entryDisplay_partOfSpeech%']}: ${partOfSpeechText(sense.partOfSpeech, analysisLanguage)}`}</span>
                </div>
              )}

              <DomainsDisplay
                analysisLanguage={analysisLanguage}
                domains={sense.semanticDomains}
                onClickDomain={onClickDomain}
              />
            </div>
          ))}
        </div>
      </div>

      <div>
        <Button
          variant="secondary"
          size="icon"
          className="tw-fixed tw-bottom-4 tw-right-4 tw-z-20"
          onClick={onClickScrollToTop}
        >
          <ChevronUpIcon />
        </Button>
      </div>
    </>
  );
}
