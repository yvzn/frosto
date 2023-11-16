import 'bootstrap/dist/css/bootstrap.min.css';

var signupForm = document.querySelector('form');
var formStatus = 'IDLE';

if (import.meta.env.MODE === "development") {
	signupForm.action = import.meta.env.VITE_SIGNUP_URL;
}

signupForm.addEventListener('submit', onFormSubmit);

function onFormSubmit(event) {
	if (formStatus == 'IDLE') {
		var button = document.querySelector('button');
		button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> Veuillez patienter...';
		formStatus = 'PENDING';
	} else {
		event.preventDefault();
	}
}

addEventListener('load', onLoad);

function onLoad() {
	var request = new XMLHttpRequest();
	var url = import.meta.env.VITE_HEALTHCHECK_URL;
	request.timeout = 10 * 1000;
	request.open("GET", url, true);
	request.send();
}
