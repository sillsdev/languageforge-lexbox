import type { IEntry, ISense } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader, Input, Label } from 'platform-bible-react';
import { type ReactElement, useCallback, useEffect, useState } from 'react';

interface AddNewEntryProps {
  addEntry: (entry: Partial<IEntry>) => Promise<void>;
  analysisLang: string;
  headword?: string;
  isAdding?: boolean;
  vernacularLang: string;
}

export default function AddNewEntry({
  addEntry,
  analysisLang,
  headword,
  isAdding,
  vernacularLang,
}: AddNewEntryProps): ReactElement {
  const [adding, setAdding] = useState(isAdding);
  const [definition, setDefinition] = useState('');
  const [gloss, setGloss] = useState('');
  const [ready, setReady] = useState(false);
  const [tempHeadword, setTempHeadword] = useState('');

  useEffect(() => setAdding(isAdding), [isAdding]);

  useEffect(() => setTempHeadword(headword || ''), [headword]);

  useEffect(() => {
    setReady(!!(tempHeadword.trim() && (gloss.trim() || definition.trim())));
  }, [definition, gloss, tempHeadword]);

  const clearEntry = useCallback((): void => {
    setAdding(isAdding);
    setDefinition('');
    setGloss('');
    setTempHeadword(headword || '');
  }, [headword, isAdding]);

  async function onSubmit(): Promise<void> {
    const entry = createEntry(
      vernacularLang,
      tempHeadword.trim(),
      analysisLang,
      gloss.trim(),
      definition.trim(),
    );
    await addEntry(entry);
    clearEntry();
  }

  return adding ? (
    <Card>
      <CardHeader>Adding new entry</CardHeader>
      <CardContent>
        <div>
          <Label htmlFor="newEntryHeadword">Headword:</Label>
          <Input
            id="newEntryHeadword"
            onChange={(e) => setTempHeadword(e.target.value)}
            value={tempHeadword}
          />
        </div>
        <div>
          <Label htmlFor="newEntryGloss">Gloss:</Label>
          <Input id="newEntryGloss" onChange={(e) => setGloss(e.target.value)} value={gloss} />
        </div>
        <div>
          <Label htmlFor="newEntryDefinition">Definition:</Label>
          <Input
            id="newEntryDefinition"
            onChange={(e) => setDefinition(e.target.value)}
            value={definition}
          />
        </div>
        <div>
          <Button disabled={!ready} onClick={() => onSubmit()}>
            Submit new entry
          </Button>
          <Button onClick={clearEntry}>Cancel</Button>
        </div>
      </CardContent>
    </Card>
  ) : (
    <Button onClick={() => setAdding(true)}>Add new entry</Button>
  );
}

function createEntry(
  vernacularLang: string,
  headword: string,
  analysisLang: string,
  gloss?: string,
  definition?: string,
): Partial<IEntry> {
  return {
    lexemeForm: { [vernacularLang]: headword },
    senses: [
      // eslint-disable-next-line no-type-assertion/no-type-assertion
      {
        definition: definition ? { [analysisLang]: definition } : {},
        gloss: gloss ? { [analysisLang]: gloss } : {},
      } as Partial<ISense> as ISense,
    ],
  };
}
