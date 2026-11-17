// Settings of the site being built (SITE=fr|en), available in templates as {{ site.* }}.
import { currentSite } from '../../scripts/lib/sites.mjs';
import en from './sites/en.js';
import fr from './sites/fr.js';

const sites = { fr, en };

export default sites[currentSite()];
