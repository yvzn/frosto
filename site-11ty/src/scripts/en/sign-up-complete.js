import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';
import './share-actions.js';

// Email address split in parts, out of reach of simple scrapers. .map(String) keeps the minifier
// (rolldown 1.2.16+) from folding the parts back into a plain string: do not remove it.
document.getElementById("ademl").innerHTML = ['yvan', '&#', '64;', 'frostalert.', 'net'].map(String).join('');
