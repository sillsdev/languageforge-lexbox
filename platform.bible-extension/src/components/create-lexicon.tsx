import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import { Button, Input, Label } from 'platform-bible-react';
import { type ReactElement, useEffect, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

const CODE_PATTERN = /^[a-z\d][a-z\d-]*$/;
const MIN_CODE_LENGTH = 4;

function isValidLangTag(tag: string): boolean {
  try {
    return Intl.getCanonicalLocales(tag).length === 1;
  } catch {
    return false;
  }
}

function deriveCode(name: string): string {
  return name
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '-')
    .replace(/-+/g, '-')
    .replace(/^[^a-z0-9]+/, '')
    .replace(/[^a-z0-9]+$/, '');
}

interface CreateLexiconProps {
  createLexicon: (
    name: string,
    code: string,
    vernacularWs: string,
    analysisWs?: string,
  ) => Promise<void>;
  defaultVernacularWs?: string;
  existingCodes?: string[];
  onCancel: () => void;
  onCreated: (code: string) => Promise<void>;
}

/** A form for creating a new FW Lite CRDT project from the blank template. */
export default function CreateLexicon({
  createLexicon,
  defaultVernacularWs,
  existingCodes,
  onCancel,
  onCreated,
}: CreateLexiconProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [codeEdited, setCodeEdited] = useState(false);
  const [vernacularWs, setVernacularWs] = useState(defaultVernacularWs ?? '');
  const [analysisWs, setAnalysisWs] = useState('');
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!codeEdited) setCode(deriveCode(name));
  }, [codeEdited, name]);

  // The code input lowercases as typed, but existingCodes may come from elsewhere with different
  // casing, so compare case-insensitively to catch a well-formed collision before it 400s.
  const codeExists =
    !!code && (existingCodes ?? []).some((c) => c.toLowerCase() === code.toLowerCase());

  const isValid = !!(
    name.trim() &&
    code.length >= MIN_CODE_LENGTH &&
    CODE_PATTERN.test(code) &&
    !codeExists &&
    isValidLangTag(vernacularWs.trim()) &&
    (!analysisWs.trim() || isValidLangTag(analysisWs.trim()))
  );

  const handleSubmit = async () => {
    if (!isValid) return;
    setCreating(true);
    setError('');
    try {
      await createLexicon(name.trim(), code, vernacularWs.trim(), analysisWs.trim() || undefined);
      await onCreated(code);
    } catch (e) {
      logger.error(localizedStrings['%lexicon_createLexicon_error%'], JSON.stringify(e));
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="tw:flex tw:flex-col tw:gap-1 tw:p-4">
      <h3 className="tw:font-semibold tw:mb-2">
        {localizedStrings['%lexicon_createLexicon_title%']}
      </h3>

      <div>
        <Label htmlFor="createLexiconName">
          {localizedStrings['%lexicon_createLexicon_name%']}
        </Label>
        <Input id="createLexiconName" onChange={(e) => setName(e.target.value)} value={name} />
      </div>

      <div>
        <Label htmlFor="createLexiconCode">
          {localizedStrings['%lexicon_createLexicon_code%']}
        </Label>
        <Input
          id="createLexiconCode"
          onChange={(e) => {
            setCode(e.target.value.toLowerCase());
            setCodeEdited(true);
          }}
          value={code}
        />
        {codeExists && (
          <p className="tw:text-sm tw:text-destructive tw:mt-1">
            {localizedStrings['%lexicon_createLexicon_codeExists%']}
          </p>
        )}
        {!codeExists && !!code && code.length < MIN_CODE_LENGTH && (
          <p className="tw:text-sm tw:text-destructive tw:mt-1">
            {localizedStrings['%lexicon_createLexicon_codeTooShort%']}
          </p>
        )}
        {!codeExists && code.length >= MIN_CODE_LENGTH && !CODE_PATTERN.test(code) && (
          <p className="tw:text-sm tw:text-destructive tw:mt-1">
            {localizedStrings['%lexicon_createLexicon_codeInvalid%']}
          </p>
        )}
      </div>

      <div>
        <Label htmlFor="createLexiconVernWs">
          {localizedStrings['%lexicon_createLexicon_vernacularWs%']}
        </Label>
        <Input
          id="createLexiconVernWs"
          onChange={(e) => setVernacularWs(e.target.value)}
          value={vernacularWs}
        />
        {!!vernacularWs.trim() && !isValidLangTag(vernacularWs.trim()) && (
          <p className="tw:text-sm tw:text-destructive tw:mt-1">
            {localizedStrings['%lexicon_createLexicon_langTagInvalid%']}
          </p>
        )}
      </div>

      <div>
        <Label htmlFor="createLexiconAnalysisWs">
          {localizedStrings['%lexicon_createLexicon_analysisWs%']}
        </Label>
        <Input
          id="createLexiconAnalysisWs"
          onChange={(e) => setAnalysisWs(e.target.value)}
          placeholder="en"
          value={analysisWs}
        />
        {!!analysisWs.trim() && !isValidLangTag(analysisWs.trim()) && (
          <p className="tw:text-sm tw:text-destructive tw:mt-1">
            {localizedStrings['%lexicon_createLexicon_langTagInvalid%']}
          </p>
        )}
      </div>

      {error && <p className="tw:text-sm tw:text-destructive tw:mt-1">{error}</p>}

      <div className="tw:flex tw:gap-1 tw:mt-2">
        <Button disabled={!isValid || creating} onClick={() => handleSubmit()}>
          {localizedStrings['%lexicon_createLexicon_submit%']}
        </Button>
        <Button onClick={onCancel} variant="secondary">
          {localizedStrings['%lexicon_button_cancel%']}
        </Button>
      </div>
    </div>
  );
}
