import { createI18n } from 'vue-i18n';

const i18n = createI18n({
	legacy: false,
	locale: 'fr',
	fallbackLocale: 'en',
	messages: {
		en: {
			app: {
				title: 'Frost Alerts',
				homePage: 'Home page',
				backToHome: 'Back to home',
				languageSwitcher: 'Change language',
			},
		},
		fr: {
			app: {
				title: 'Alerte gelées',
				homePage: 'Accueil',
				backToHome: "Retour à l'accueil",
				languageSwitcher: 'Changer de langue',
			},
		},
	},
});

export default i18n;
