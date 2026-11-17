import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';

// Email address split in parts, out of reach of simple scrapers. .map(String) keeps the minifier
// (rolldown 1.2.16+) from folding the parts back into a plain string: do not remove it.
document.getElementById("ademl").innerHTML = '<a class="btn btn-primary my-2" href="mai' + 'lto:' + ['yvan', '%40', 'alertegelee.', 'fr'].map(String).join('') + '">Contact us</a>';
