import 'bootstrap/dist/css/bootstrap.min.css';

const signupForm = document.querySelector('form');

if (import.meta.env.MODE === "development") {
	signupForm.action = import.meta.env.VITE_SIGNUP_URL;
}

signupForm.addEventListener('submit', onFormSubmit);

function onFormSubmit() {
	const button = document.querySelector('button');
	button.disabled = 'true';
	button.innerHTML = '<span class="spinner-border spinner-border-sm fs-6" role="status" aria-hidden="true"></span> Veuillez patienter...';
}
