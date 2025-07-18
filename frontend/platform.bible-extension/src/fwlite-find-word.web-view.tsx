import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { FindWebViewOptions, IEntry, IEntryService, ISense } from 'fw-lite-extension';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  Input,
  Label,
  SearchBar,
} from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect({
  projectId,
  word,
}: FindWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    void papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      });
  }, []);

  const fetchEntries = useCallback(
    async (word: string) => {
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }

      word = word.trim();
      if (!word) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${word}`);
      setIsFetching(true);
      const entries = await fwLiteNetworkObject.getEntries(projectId, { surfaceForm: word });
      setIsFetching(false);
      setMatchingEntries(entries ?? []);
    },
    [fwLiteNetworkObject, projectId],
  );

  const addEntry = useCallback(
    async (entry: IEntry) => {
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      await fwLiteNetworkObject.addEntry(projectId, entry);
    },
    [fwLiteNetworkObject, projectId],
  );

  const debouncedFetchEntries = useMemo(() => debounce(fetchEntries, 500), [fetchEntries]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      debouncedFetchEntries(searchQuery);
    },
    [debouncedFetchEntries],
  );

  return (
    <div>
      <SearchBar placeholder="Find in dictionary..." value={searchTerm} onSearch={onSearch} />

      {isFetching && <p>Loading...</p>}
      {!matchingEntries?.length && !isFetching && <p>No matching entries</p>}
      {matchingEntries?.map((entry) => (
        <Card key={entry.id}>
          <CardHeader>
            {Object.keys(entry.citationForm).length
              ? JSON.stringify(entry.citationForm)
              : JSON.stringify(entry.lexemeForm)}
          </CardHeader>
          <CardContent>
            <p>Senses:</p>
            {entry.senses.map((sense) => (
              <div key={sense.id}>
                <strong>Gloss: {JSON.stringify(sense.gloss)}</strong>
                <p>Definition: {JSON.stringify(sense.definition)}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      ))}

      <AddNewEntry
        addEntry={addEntry}
        analysisLang="en"
        headword={searchTerm}
        vernacularLang="qaa"
      />
    </div>
  );
};

interface AddNewEntryProps {
  addEntry: (entry: IEntry) => Promise<void>;
  analysisLang: string;
  headword?: string;
  vernacularLang: string;
}

function AddNewEntry(props: AddNewEntryProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [isReady, setIsReady] = useState(false);

  const [headword, setHeadword] = useState('');
  const [gloss, setGloss] = useState('');
  const [definition, setDefinition] = useState('');

  useEffect(() => setHeadword(props.headword || ''), [props.headword]);

  useEffect(() => {
    setIsReady(!!(headword.trim() && (gloss.trim() || definition.trim())));
  }, [definition, gloss, headword]);

  async function addEntry(): Promise<void> {
    const sense = CreateSense(props.analysisLang, gloss.trim(), definition.trim());
    const entry = CreateEntry(props.vernacularLang, headword.trim(), sense);
    await props.addEntry(entry);
  }

  const clearEntry = useCallback((): void => {
    setIsAdding(false);
    setHeadword(props.headword || '');
    setGloss('');
    setDefinition('');
  }, [props.headword]);

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

function CreateSense(lang: string, gloss?: string, definition?: string): ISense {
  return {
    definition: definition ? { [lang]: definition } : {},
    entryId: '',
    exampleSentences: [],
    gloss: gloss ? { [lang]: gloss } : {},
    id: '',
    semanticDomains: [],
  };
}

function CreateEntry(lang: string, headword: string, sense: ISense): IEntry {
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
