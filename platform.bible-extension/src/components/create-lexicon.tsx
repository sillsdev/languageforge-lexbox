import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import { Button, Input, Label } from 'platform-bible-react';
import { type ReactElement, useCallback, useEffect, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

const CODE_PATTERN = /^[a-z\d][a-z\d-]*$/;

function deriveCode(name: string): string {
  return name
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
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
  onCancel: () => void;
  onCreated: (code: string) => Promise<void>;
}

/** A form for creating a new FW Lite CRDT project from the blank template. */
export default function CreateLexicon({
  createLexicon,
  defaultVernacularWs,
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

  useEffect(() => {
    if (!codeEdited) setCode(deriveCode(name));
  }, [codeEdited, name]);

  const isValid = !!(name.trim() && CODE_PATTERN.test(code) && vernacularWs.trim());

  const handleSubmit = useCallback(async () => {
    if (!isValid) return;
    setCreating(true);
    try {
      await createLexicon(name.trim(), code, vernacularWs.trim(), analysisWs.trim() || undefined);
      await onCreated(code);
    } catch (e) {
      logger.error(localizedStrings['%lexicon_createLexicon_error%'], JSON.stringify(e));
    } finally {
      setCreating(false);
    }
  }, [analysisWs, code, createLexicon, isValid, localizedStrings, name, onCreated, vernacularWs]);

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
            setCode(e.target.value);
            setCodeEdited(true);
          }}
          value={code}
        />
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
      </div>

      <div>
        <Label htmlFor="createLexiconAnalysisWs">
          {localizedStrings['%lexicon_createLexicon_analysisWs%']}
        </Label>
        <Input
          id="createLexiconAnalysisWs"
          onChange={(e) => setAnalysisWs(e.target.value)}
          value={analysisWs}
        />
      </div>

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
