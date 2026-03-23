import { createI18n } from 'vue-i18n';

const i18n = createI18n({
	legacy: false,
	locale: navigator.language.split('-')[0],
	fallbackLocale: 'en',
	messages: {
		en: {
			app: {
				title: 'Frost Alerts',
				description:
					'Get frost alerts near me, frost alerts for my area, and tips so you can prepare in time.',
				homePage: 'Home page',
				backToHome: 'Back to home',
				languageSwitcher: 'Change language',
			},
			welcome: {
				heading: 'Anticipate frost',
				lead: 'Receive a free alert when the weather forecast predicts negative temperatures in your area.',
				verifyEmail: 'Please verify your email address to continue.',
				cta: 'Check Subscription',
				imageAlt: 'A person observing weather change icons',
			},
			checkSubscription: {
				pageTitle: 'Check My Subscription',
				heading: 'Check My Subscription',
				lead: 'Enter your email address to verify if you are subscribed to our frost alert service.',
				description: "We'll check our records and confirm your subscription status.",
				noSubscription: "If you're not subscribed yet,",
				signUpLink: 'use this form to subscribe for free.',
				formLabel: 'Check subscription form',
				emailLabel: 'Email address',
				emailPlaceholder: 'address%40example.com',
				reasonLabel: 'Reason for checking',
				reasonPlaceholder: 'example: I want to confirm my subscription',
				consent: 'I agree that my personal contact details are used as part of this service.',
				submit: 'Check Subscription',
				pleaseWait: 'Please wait...',
				takingLonger: 'This is taking longer than expected...',
				failureMessage: 'There was a problem checking your subscription.',
				retry: 'Retry by refreshing the page',
				legalText:
					'FrostAlert.net processes the collected data to analyze weather forecasts and send alerts.',
				legalLink:
					'To learn more about the management of your personal data and to exercise your rights, consult the {link}.',
				legalLinkText: 'service terms of use',
			},
			checkSubscriptionComplete: {
				pageTitle: 'Subscription Check Submitted',
				heading: 'Subscription check submitted',
				lead: 'We will send you an email shortly to confirm your subscription and finalize the process.',
				imageAlt: 'An operator reviewing user preferences',
			},
		},
		fr: {
			app: {
				title: 'Alerte gelées',
				description:
					'Le service alerte gel météo vous envoie gratuitement une notification si des températures négatives sont prévues dans les jours suivants',
				homePage: 'Accueil',
				backToHome: "Retour à l'accueil",
				languageSwitcher: 'Changer de langue',
			},
			welcome: {
				heading: 'Anticipez les gelées',
				lead: 'Recevez gratuitement une alerte lorsque la météo prévoit des températures négatives dans votre ville.',
				verifyEmail: 'Veuillez vérifier votre adresse e-mail pour continuer.',
				cta: 'Vérifier mon inscription',
				imageAlt: 'Une personne observe des icônes de changement de météo',
			},
			checkSubscription: {
				pageTitle: 'Vérifier mon inscription',
				heading: 'Vérifier mon inscription',
				lead: "Entrez votre adresse e-mail pour vérifier si vous êtes abonné à notre service d'alerte gel.",
				description: 'Nous vous enverrons un e-mail pour confirmer votre inscription.',
				noSubscription: "Si vous n'êtes pas encore inscrit,",
				signUpLink: 'utilisez ce formulaire pour vous inscrire gratuitement.',
				formLabel: "Formulaire de vérification d'inscription",
				emailLabel: 'Adresse e-mail',
				emailPlaceholder: 'adresse%40example.com',
				reasonLabel: 'Raison de la vérification',
				reasonPlaceholder: 'exemple : Je souhaite confirmer mon inscription',
				consent:
					"J'accepte que mes coordonnées personnelles soient utilisées dans le cadre de ce service.",
				submit: 'Vérifier',
				pleaseWait: 'Veuillez patienter...',
				takingLonger: 'Cela prend plus de temps que prévu...',
				failureMessage: 'Un problème est survenu lors de la vérification de votre inscription.',
				retry: 'Réessayer en rafraîchissant la page',
				legalText:
					'AlerteGelee.fr traite les données collectées pour analyser les prévisions météo et envoyer des alertes.',
				legalLink:
					'Pour en savoir plus sur la gestion de vos données personnelles et exercer vos droits, consultez les {link}.',
				legalLinkText: "conditions générales d'utilisation du service",
			},
			checkSubscriptionComplete: {
				pageTitle: "Vérification d'inscription soumise",
				heading: 'Vérification soumise',
				lead: 'Nous vous enverrons un e-mail prochainement pour confirmer votre inscription et finaliser le processus.',
				imageAlt: 'Un opérateur examinant les préférences utilisateur',
			},
		},
	},
});

export default i18n;
