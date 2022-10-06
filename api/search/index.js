module.exports = async function (context, req) {
	console.log("q=" + req.query.q);

	const responseMessage = [
		{
			city: "Nancy",
			country: "France",
			latitude: "47.218371",
			longitude: "-1.553621",
		},
		{
			city: "Nanterre",
			country: "France",
			latitude: "48.913458",
			longitude: "2.307543",
		},
		{
			city: "Nantes en Ratier",
			country: "France",
			latitude: "44.944433",
			longitude: "5.893312",
		},
		{
			city: "Nancy",
			country: "France",
			latitude: "48.726806",
			longitude: "6.291872",
		},
	];

	context.res = {
		body: responseMessage,
		headers: {
			"Content-type": "application/json",
		},
	};
};
