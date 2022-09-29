<template>
	<section class="container py-5">
		<div class="row flex-lg-row-reverse align-items-center py-lg-5">
			<article class="col-10 col-sm-8 col-lg-6 p-4 p-md-5">
				<img
					src="/undraw_weather_re_qsmd.svg"
					class="d-block mx-lg-auto img-fluid"
					alt="Illustration d'une personne observant des icônes de changement de météo"
					width="600"
					height="278"
				/>
			</article>
			<article class="col-lg-6 p-4 p-md-5">
				<h1 class="fw-light">Anticipez les gelées</h1>
				<p class="lead text-muted">
					Recevez une alerte lorsque la météo prévoit des températures négatives
					dans votre ville.
				</p>
				<p>
					<RouterLink class="btn btn-primary my-2" to="/search"
						>Commencer</RouterLink
					>
				</p>
			</article>
		</div>
	</section>
	<section class="py-5 bg-light">
		<div class="container">
			<div class="row">
				<article class="col-lg-4">
					<h2>Personnalisé</h2>
					<p>
						Protégez-vous contre les éventuels dégâts occasionés par le
						gel&nbsp;: une alerte vous est envoyée à l'avance, automatiquement,
						lorsqu'un épisode de gel est prévu.
					</p>
				</article>
				<article class="col-lg-4">
					<h2>Gratuit</h2>
					<p>
						Nous analysons pour vous les prévisions météo des prochains jours,
						gratuitement.
					</p>
				</article>
				<article class="col-lg-4">
					<h2>Simple</h2>
					<p>
						Renseignez vos coordonnées et votre ville, vous serez alerté
						uniquement en cas de gelées, pas d'autres sollicitations
						commerciales.
					</p>
				</article>
			</div>
		</div>
	</section>
	<footer class="text-muted py-5">
		<div class="container">
			<p>
				Les données météo sont fournies par <em>Microsoft</em> et
				<em>AccuWeather</em>. Nous nous efforçons de protéger au mieux les
				données personnelles que vous nous confiez.
			</p>
			<p>
				Service
				<a href="https://github.com/yvzn/frosto/" target="_blank" rel="noopener"
					>open source</a
				>
				maintenu par des
				<a href="mailto:team-frosto[at]outlook.fr">bénévoles</a>. Illustrations
				par
				<a href="https://undraw.co/" target="_blank" rel="noopener">unDraw</a>.
			</p>
		</div>
	</footer>
</template>
