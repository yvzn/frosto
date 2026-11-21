import { recordDuration } from './form-duration.js';

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

	var email = feedbackForm.elements.namedItem('email');
	var consentContainer = document.querySelector('#feedback-consent');
	var consent = feedbackForm.elements.namedItem('consent');

	function updateConsent() {
		var visible = email.value.trim().length > 0;
		consentContainer.hidden = !visible;
		consent.required = visible;
		if (!visible) consent.checked = false;
	}

	email.addEventListener('input', updateConsent);
	updateConsent();

	healthCheck(3);

	function healthCheck(retries) {
		var request = new XMLHttpRequest();
		request.timeout = 30 * 1000;
		request.ontimeout = function () {
			if (retries > 1) healthCheck(retries - 1);
		};
		request.onerror = function () {
			if (retries > 1) healthCheck(retries - 1);
		};
		request.open('GET', import.meta.env.VITE_HEALTHCHECK_URL, true);
		request.send();
	}
}
