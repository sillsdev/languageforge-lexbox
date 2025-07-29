import type { IEntry, IMultiString, ISemanticDomain } from 'fw-lite-extension';
import { Button, Card, CardContent, CardHeader } from 'platform-bible-react';

function domainText(domain: ISemanticDomain, lang = 'en'): string {
  // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
  return `${domain.code}: ${domain.name[lang] || domain.name['en']}`;
}

interface EntryCardProps {
  entry: IEntry;
  onClickSemanticDomain?: (domain: ISemanticDomain) => void;
}

// eslint-disable-next-line @typescript-eslint/naming-convention
export default function EntryCard({ entry, onClickSemanticDomain }: EntryCardProps) {
  return (
    <Card key={entry.id}>
      <CardHeader>
        {Object.keys(entry.citationForm as IMultiString).length
          ? JSON.stringify(entry.citationForm)
          : JSON.stringify(entry.lexemeForm)}
      </CardHeader>
      <CardContent>
        <p>Senses:</p>
        {entry.senses.map((sense) => (
          <div key={sense.id}>
            <strong>Gloss: {JSON.stringify(sense.gloss)}</strong>
            <p>Definition: {JSON.stringify(sense.definition)}</p>
            <p>Part of speech: {JSON.stringify(sense.partOfSpeech)}</p>
            <p>
              Semantic Domains:
              {sense.semanticDomains.map((dom) =>
                onClickSemanticDomain ? (
                  <Button onClick={() => onClickSemanticDomain(dom)}>{domainText(dom)}</Button>
                ) : (
                  ' ' + domainText(dom)
                ),
              )}
            </p>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
