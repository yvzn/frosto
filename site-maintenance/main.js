import 'bootstrap/dist/css/bootstrap.min.css';

if (import.meta.env.MODE === "development") {
	const button = document.querySelector('a.btn-primary');
	button.href = import.meta.env.VITE_HOMEPAGE_URL;
}
