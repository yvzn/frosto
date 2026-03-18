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
		},
	},
});

export default i18n;
