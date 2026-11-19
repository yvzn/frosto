// Wakes up the Azure Functions app behind a form, to avoid a cold start when the form is submitted.
//
// A first ping is sent when the page has loaded. A second one is sent when the visitor first focuses
// a field of the form: the app may have gone idle again if the page was left open for a while.
//
// Pings are fire and forget: the response is ignored, reaching the app is what wakes it up.

var RETRIES = 3;
var TIMEOUT_MS = 30 * 1000;
// A network error comes back at once: wait a bit before trying again.
var ERROR_RETRY_DELAY_MS = 2 * 1000;

function ping(url, retries) {
	var request = new XMLHttpRequest();

	request.timeout = TIMEOUT_MS;
	// The timeout is already a long wait: retry right away.
	request.ontimeout = function () {
		if (retries > 1) {
			ping(url, retries - 1);
		}
	};
	request.onerror = function () {
		if (retries > 1) {
			setTimeout(function () {
				ping(url, retries - 1);
			}, ERROR_RETRY_DELAY_MS);
		}
	};

	request.open('GET', url, true);
	request.send();
}

export function warmUp(url, form) {
	addEventListener('load', function () {
		ping(url, RETRIES);
	});

	form.addEventListener(
		'focusin',
		function () {
			ping(url, RETRIES);
		},
		{ once: true },
	);
}
