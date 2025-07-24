import type { IEntry, ISense } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader, Input, Label } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';

interface AddNewEntryProps {
  addEntry: (entry: Partial<IEntry>) => Promise<void>;
  analysisLang: string;
  headword?: string;
  isAdding?: boolean;
  vernacularLang: string;
}

// eslint-disable-next-line @typescript-eslint/naming-convention
export default function AddNewEntry(props: AddNewEntryProps) {
  const [isAdding, setIsAdding] = useState(props.isAdding);
  const [isReady, setIsReady] = useState(false);

  const [headword, setHeadword] = useState('');
  const [gloss, setGloss] = useState('');
  const [definition, setDefinition] = useState('');

  useEffect(() => setHeadword(props.headword || ''), [props.headword]);

  useEffect(() => {
    setIsReady(!!(headword.trim() && (gloss.trim() || definition.trim())));
  }, [definition, gloss, headword]);

  async function addEntry(): Promise<void> {
    const entry = createEntry(
      props.vernacularLang,
      headword.trim(),
      props.analysisLang,
      gloss.trim(),
      definition.trim(),
    );
    await props.addEntry(entry);
    clearEntry();
  }

  const clearEntry = useCallback((): void => {
    setIsAdding(props.isAdding);
    setHeadword(props.headword || '');
    setGloss('');
    setDefinition('');
  }, [props.headword, props.isAdding]);

  return isAdding ? (
    <Card>
      <CardHeader>Adding new entry</CardHeader>
      <CardContent>
        <div>
          <Label htmlFor="newEntryHeadword">Headword:</Label>
          <Input
            id="newEntryHeadword"
            onChange={(e) => void setHeadword(e.target.value)}
            value={headword}
          />
        </div>
        <div>
          <Label htmlFor="newEntryGloss">Gloss:</Label>
          <Input id="newEntryGloss" onChange={(e) => void setGloss(e.target.value)} value={gloss} />
        </div>
        <div>
          <Label htmlFor="newEntryDefinition">Definition:</Label>
          <Input
            id="newEntryDefinition"
            onChange={(e) => void setDefinition(e.target.value)}
            value={definition}
          />
        </div>
        <div>
          <Button disabled={!isReady} onClick={() => void addEntry()}>
            Submit new entry
          </Button>
          <Button onClick={clearEntry}>Cancel</Button>
        </div>
      </CardContent>
    </Card>
  ) : (
    <Button onClick={() => void setIsAdding(true)}>Add new entry</Button>
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
      {
        definition: definition ? { [analysisLang]: definition } : {},
        gloss: gloss ? { [analysisLang]: gloss } : {},
      } as Partial<ISense> as ISense,
    ],
  };
}
