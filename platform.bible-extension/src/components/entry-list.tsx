import type { IEntry } from 'fw-lite-extension';
import type { ReactElement } from 'react';
import EntryCard from './entry-card';

interface EntryListProps {
  entries: IEntry[];
}

// Match className from paranext-core/extensions/src/components/dictionary/dictionary-list.component.tsx
export default function EntryList({ entries }: EntryListProps): ReactElement {
  return (
    <div className="tw-flex tw-flex-row tw-flex-1 tw-overflow-hidden">
      <div className="tw-overflow-y-auto tw-px-2 tw-py-2 tw-w-full">
        <ul className="tw-outline-none focus:tw-ring-2 focus:tw-ring-ring focus:tw-ring-offset-1 focus:tw-ring-offset-background">
          {entries.map((entry) => (
            <EntryCard entry={entry} key={entry.id} />
          ))}
        </ul>
      </div>
    </div>
  );
}
