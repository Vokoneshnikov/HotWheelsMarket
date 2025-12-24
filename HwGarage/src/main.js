import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import './style.css';
// import './assets/main.css'; // оставь или поправь путь, если файла нет
createApp(App)
    .use(router)
    .mount('#app');
