import 'bootstrap/dist/css/bootstrap.min.css';

if (import.meta.env.MODE === "development") {
	const signupForm = document.querySelector('form');
	if (signupForm) {
		signupForm.action = import.meta.env.VITE_SIGNUP_URL;
	}
}
