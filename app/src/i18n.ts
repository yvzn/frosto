import { createI18n } from 'vue-i18n';

const i18n = createI18n({
	legacy: false,
	locale: 'fr',
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
				verifyEmail: 'Please verify your email address to get full access to your subscription.',
				cta: 'Check Subscription',
				imageAlt: 'A person observing weather change icons',
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
				verifyEmail: 'Veuillez vérifier votre adresse e-mail pour accéder pleinement à votre inscription.',
				cta: 'Vérifier mon inscription',
				imageAlt: 'Une personne observe des icônes de changement de météo',
			},
		},
	},
});

export default i18n;
