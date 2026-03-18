import './assets/main.scss';
import { createHead } from '@unhead/vue/client';
// oxlint-disable-next-line no-unused-vars
import { Dropdown } from 'bootstrap';
import { createApp } from 'vue';

import App from './App.vue';
import i18n from './i18n';
import router from './router';

const app = createApp(App);
const head = createHead();

app.use(i18n);
app.use(head);
app.use(router);

app.mount('#app');
