import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';
import { initShareActions } from './share-actions.js';

initShareActions({
	title: 'Frost Alerts',
	text: 'A free service to get notified before frost.',
	copied: 'Link copied',
	copyFailed: 'Unable to copy the link',
});

// Email address split in parts, out of reach of simple scrapers. .map(String) keeps the minifier
// (rolldown 1.2.16+) from folding the parts back into a plain string: do not remove it.
document.getElementById("ademl").innerHTML = ['yvan', '&#', '64;', 'frostalert.', 'net'].map(String).join('');
