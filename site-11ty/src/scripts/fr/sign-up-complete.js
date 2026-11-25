import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';
import { initShareActions } from './share-actions.js';

initShareActions({
	title: 'Alerte gelées',
	text: 'Un service gratuit pour être prévenu avant les gelées.',
	copied: 'Lien copié',
	copyFailed: 'Impossible de copier le lien',
});

// Email address split in parts, out of reach of simple scrapers. .map(String) keeps the minifier
// (rolldown 1.2.16+) from folding the parts back into a plain string: do not remove it.
document.getElementById("ademl").innerHTML = ['yvan', '&#', '64;', 'alertegelees.', 'fr'].map(String).join('');
