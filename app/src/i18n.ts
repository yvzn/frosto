import { createI18n } from 'vue-i18n';

const i18n = createI18n({
	locale: 'fr',
	fallbackLocale: 'en',
	messages: {
		en: {
			app: {
				title: 'Frost Alerts',
				'language switcher': 'Language:',
			},
		},
		fr: {
			app: {
				title: 'Alerte gelées',
				'language switcher': 'Langue :',
			},
		},
	},
});

export default i18n;
