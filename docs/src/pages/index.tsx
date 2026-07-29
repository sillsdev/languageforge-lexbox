import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

const sections = [
  {to: '/user-guide/', label: 'User guide'},
  {to: '/technical/', label: 'Technical documentation'},
];

const Home = (): ReactNode => {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout description={siteConfig.tagline}>
      <main className="container margin-vert--xl">
        <Heading as="h1">{siteConfig.title}</Heading>
        <p>{siteConfig.tagline}</p>
        <div className="row margin-top--lg">
          {sections.map((section) => (
            <div key={section.to} className="col col--6 margin-bottom--md">
              <Link to={section.to} className={`card padding--lg ${styles.sectionCard}`}>
                <Heading as="h2" className="margin-bottom--none">
                  {section.label}
                </Heading>
              </Link>
            </div>
          ))}
        </div>
      </main>
    </Layout>
  );
};

export default Home;
