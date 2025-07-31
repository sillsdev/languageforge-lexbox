import type { IEntry, IPartOfSpeech, ISemanticDomain } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader } from 'platform-bible-react';
import type { ReactElement } from 'react';

function domainText(domain: ISemanticDomain, lang = 'en'): string {
  return `${domain.code}: ${domain.name[lang] || domain.name.en}`;
}

function partOfSpeechText(partOfSpeech: IPartOfSpeech, lang = 'en'): string {
  return partOfSpeech.name[lang] || partOfSpeech.name.en;
}

interface EntryCardProps {
  entry: IEntry;
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
}

export default function EntryCard({ entry, onClickSemanticDomain }: EntryCardProps): ReactElement {
  return (
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
            {sense.partOfSpeech && <p>Part of speech: {partOfSpeechText(sense.partOfSpeech)}</p>}
            <p>
              Semantic Domains:
              {sense.semanticDomains.map((dom) =>
                onClickSemanticDomain ? (
                  <Button onClick={() => onClickSemanticDomain(dom)}>{domainText(dom)}</Button>
                ) : (
                  ` ${domainText(dom)}`
                ),
              )}
            </p>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
