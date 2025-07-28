import type { IEntry, IMultiString } from 'fw-lite-extension';
import { Card, CardContent, CardHeader } from 'platform-bible-react';

interface EntryCardProps {
  entry: IEntry;
}

// eslint-disable-next-line @typescript-eslint/naming-convention
export default function EntryCard({ entry }: EntryCardProps) {
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
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
