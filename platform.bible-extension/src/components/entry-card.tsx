import type { IEntry, ISemanticDomain } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader } from 'platform-bible-react';
import type { ReactElement } from 'react';
import { domainText, entryHeadwordText, partOfSpeechText } from '../utils/entry-display-text';

interface EntryCardProps {
  entry: IEntry;
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
}

export default function EntryCard({ entry, onClickSemanticDomain }: EntryCardProps): ReactElement {
  return (
    <Card key={entry.id}>
      <CardHeader>{entryHeadwordText(entry)}</CardHeader>
      <CardContent>
        <p>Senses:</p>
        {entry.senses.map((sense) => (
          <div key={sense.id}>
            <strong>Gloss: {JSON.stringify(sense.gloss)}</strong>
            <p>Definition: {JSON.stringify(sense.definition)}</p>
            {sense.partOfSpeech && <p>Part of speech: {partOfSpeechText(sense.partOfSpeech)}</p>}
            <p>
              Semantic Domains:
              {sense.semanticDomains.map((dom) =>
                onClickSemanticDomain ? (
                  <Button key={dom.code} onClick={() => onClickSemanticDomain(dom)}>
                    {domainText(dom)}
                  </Button>
                ) : (
                  <span key={dom.code}> {domainText(dom)}</span>
                ),
              )}
            </p>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
