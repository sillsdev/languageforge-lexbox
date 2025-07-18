import type { IEntry, ISense } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader, Input, Label } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';

interface AddNewEntryProps {
  addEntry: (entry: IEntry) => Promise<void>;
  analysisLang: string;
  headword?: string;
  isAdding?: boolean;
  vernacularLang: string;
}

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
    const sense = createSense(props.analysisLang, gloss.trim(), definition.trim());
    const entry = createEntry(props.vernacularLang, headword.trim(), sense);
    await props.addEntry(entry);
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
            onChange={(e) => setHeadword(e.target.value)}
            value={headword}
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

function createSense(lang: string, gloss?: string, definition?: string): ISense {
  return {
    definition: definition ? { [lang]: definition } : {},
    entryId: '',
    exampleSentences: [],
    gloss: gloss ? { [lang]: gloss } : {},
    id: '',
    semanticDomains: [],
  };
}

function createEntry(lang: string, headword: string, sense: ISense): IEntry {
  return {
    citationForm: { [lang]: headword },
    complexForms: [],
    complexFormTypes: [],
    components: [],
    id: sense.entryId || '',
    lexemeForm: {},
    literalMeaning: {},
    note: {},
    publishIn: [],
    senses: [sense],
  };
}
