import 'bootstrap/dist/css/bootstrap.min.css';
import './main.css';

var unsubscribeParams = [
	'user',
	'email',
	'id',
	'reason',
	'origin',
];
var queryParams = new URLSearchParams(window.location.search);

for (const parameter of unsubscribeParams) {
	if (queryParams.has(parameter)) {
		var input = document.getElementById(parameter);
		if (input) {
			input.value = queryParams.get(parameter);
		}
	}
}

(function() {
	var fixAccessibility = document.getElementsByTagName('input')[1].parentElement.style;
	fixAccessibility.position = 'absolute';
	fixAccessibility.width = '1px';
	fixAccessibility.height = '1px';
	fixAccessibility.overflow = 'hidden';
})();
