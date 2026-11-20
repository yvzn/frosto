import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';
import { warmUp } from './warm-up.js';
import { recordDuration } from './form-duration.js';

var checkForm = document.querySelector('form');
var formStatus = 'IDLE';

if (import.meta.env.MODE === "development") {
	checkForm.action = import.meta.env.VITE_CHECKSUBSCRIPTION_URL;
}

recordDuration(checkForm);
checkForm.addEventListener('submit', showLoadingText);

function showLoadingText(event) {
	if (formStatus == 'IDLE') {
		var button = document.querySelector('button');
		button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> Veuillez patienter...';
		formStatus = 'PENDING';

		updateLoadingText.timeoutId = setTimeout(updateLoadingText, 10_000);

		// trick to allow setTimeout during form submission
		event.preventDefault();
		checkForm.submit();
	} else {
		event.preventDefault();
	}
}

function updateLoadingText() {
	var button = document.querySelector('button');
	button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> Cela prend plus de temps que prévu...';

	showFailureText.timeoutId = setTimeout(showFailureText, 50_000);
}

function showFailureText() {
	var button = document.querySelector('button');

	var failureMessage = document.createElement('div');
	failureMessage.className = 'alert alert-danger mt-3';
	failureMessage.role = 'alert';
	failureMessage.innerText = "Un problème est survenu lors de la vérification de votre inscription.";

	button.insertAdjacentElement('beforebegin', failureMessage);

	button.innerHTML = 'Réessayer en rafraîchissant la page';
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

warmUp(import.meta.env.VITE_SUPPORT_URL, checkForm);
