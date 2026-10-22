import type {ReactNode} from 'react';

// Paths copied from the MDI icons the FW Lite viewer uses (e.g. `i-mdi-layers`).
const paths = {
  layers: 'm12 16l7.36-5.73L21 9l-9-7l-9 7l1.63 1.27M12 18.54l-7.38-5.73L3 14.07l9 7l9-7l-1.63-1.27z',
};

export default function AppIcon({name, label}: {name: keyof typeof paths; label: string}): ReactNode {
  return (
    <svg
      viewBox="0 0 24 24"
      width="1.2em"
      height="1.2em"
      role="img"
      aria-label={label}
      style={{verticalAlign: '-0.2em'}}>
      <path fill="currentColor" d={paths[name]} />
    </svg>
  );
}
