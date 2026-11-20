// Sends, along with the form input, the time spent on the page before the form was submitted.
//
// The value is in milliseconds and goes in the form's `duration` field. Call this before adding
// other submit listeners, so the value is set before the form is sent.

export function recordDuration(form) {
	var startedAt = performance.now();

	form.addEventListener('submit', function () {
		var field = form.elements.namedItem('duration');
		if (field) {
			field.value = String(Math.round(performance.now() - startedAt));
		}
	});
}
