import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

type Section = {to: string; title: string; description: string};

const products: Section[] = [
  {
    to: '/fw-lite/',
    title: 'FieldWorks Lite',
    description:
      'Build your dictionary on a phone, tablet or computer. Start here if you use the FieldWorks Lite app.',
  },
  {
    to: '/lexbox/',
    title: 'Lexbox',
    description:
      'Host your team’s projects online: Send/Receive, project members and roles, new projects, and turning on FieldWorks Lite for your team.',
  },
];

const more: Section[] = [
  {
    to: '/fw-lite/how-sync-works',
    title: 'How sync works',
    description:
      'Follow an edit from FieldWorks Lite through Lexbox to a colleague’s FieldWorks Classic, step by step.',
  },
  {
    to: '/technical/',
    title: 'Technical documentation',
    description: 'How it works under the hood: architecture and sync internals.',
  },
];

const SectionCards = ({sections, secondary = false}: {sections: Section[]; secondary?: boolean}) => (
  <div className="row">
    {sections.map((section) => (
      <div key={section.to} className="col col--6 margin-bottom--lg">
        <Link
          to={section.to}
          className={`card padding--lg ${styles.sectionCard} ${secondary ? styles.secondary : ''}`}
        >
          <Heading as="h2" className={secondary ? styles.secondaryTitle : undefined}>
            {section.title}
          </Heading>
          <p className="margin-bottom--none">{section.description}</p>
        </Link>
      </div>
    ))}
  </div>
);

const Home = (): ReactNode => {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout description={siteConfig.tagline}>
      <main className="container margin-vert--xl">
        <Heading as="h1">{siteConfig.title}</Heading>
        <p>{siteConfig.tagline}</p>
        <div className="margin-top--lg">
          <SectionCards sections={products} />
          <SectionCards sections={more} secondary />
        </div>
      </main>
    </Layout>
  );
};

export default Home;
