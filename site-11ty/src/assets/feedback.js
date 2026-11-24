import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';
import { recordDuration } from './form-duration.js';
import { warmUp } from './warm-up.js';

var feedbackForm = document.querySelector('#feedback-form');
if (feedbackForm) {
	recordDuration(feedbackForm);

	var parameters = new URLSearchParams(location.search);
	if (parameters.get('thanks') === '1') {
		document.querySelector('#feedback-form-section')?.setAttribute('hidden', '');
		document.querySelector('#feedback-thanks')?.removeAttribute('hidden');
	}

	var sentiment = parameters.get('s');
	if (sentiment === 'positive' || sentiment === 'negative') {
		var radio = feedbackForm.querySelector('[name="sentiment"][value="' + sentiment + '"]');
		if (radio) radio.checked = true;
	}

	if (!parameters.has('p') || !parameters.has('r')) {
		feedbackForm.elements.namedItem('source').value = 'home';
	}
	else {
		feedbackForm.elements.namedItem('source').value = 'alert-email';
		feedbackForm.elements.namedItem('locationPartitionKey').value = parameters.get('p') || '';
		feedbackForm.elements.namedItem('locationRowKey').value = parameters.get('r') || '';
	}

	warmUp(import.meta.env.VITE_SUPPORT_URL, feedbackForm);
}
