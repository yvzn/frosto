import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';

var signupForm = document.querySelector('form');
var formStatus = 'IDLE';

if (import.meta.env.MODE === "development") {
	signupForm.action = import.meta.env.VITE_SIGNUP_URL;
}

signupForm.addEventListener('submit', showLoadingText);

function showLoadingText(event) {
	if (formStatus == 'IDLE') {
		var button = document.querySelector('button');
		button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> Please wait...';
		formStatus = 'PENDING';

		updateLoadingText.timeoutId = setTimeout(updateLoadingText, 10_000);

		// trick to allow setTimeout during form submission
		event.preventDefault();
		signupForm.submit();
	} else {
		event.preventDefault();
	}
}

function updateLoadingText() {
	var button = document.querySelector('button');
	button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> This is taking longer than expected...';

	showFailureText.timeoutId = setTimeout(showFailureText, 50_000);
}

function showFailureText() {
	var button = document.querySelector('button');

	var failureMessage = document.createElement('div');
	failureMessage.className = 'alert alert-danger mt-3';
	failureMessage.role = 'alert';
	failureMessage.innerText = "There was a problem during the sign-up.";

	button.insertAdjacentElement('beforebegin', failureMessage);

	button.innerHTML = 'Retry by refreshing the page';
	button.addEventListener('click', function (event) {
		event.preventDefault();
		location.reload();
	});
}

addEventListener('beforeunload', clearLoadingTimeouts);

function clearLoadingTimeouts() {
	if (updateLoadingText.timeoutId) {
		clearTimeout(updateLoadingText.timeoutId);
	}
	if (showFailureText.timeoutId) {
		clearTimeout(showFailureText.timeoutId);
	}
}

addEventListener('load', onLoad);

function onLoad() {
	healthCheck(3);
}

function healthCheck(retries) {
	var request = new XMLHttpRequest();
	var url = import.meta.env.VITE_HEALTHCHECK_URL;

	request.timeout = 30 * 1000;
	request.ontimeout = function () {
		if (retries > 1) {
			healthCheck(retries - 1);
		}
	};
	request.onerror = function () {
		if (retries > 1) {
			healthCheck(retries - 1);
		}
	};

	request.open("GET", url, true);
	request.send();
}
