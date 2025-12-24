import { createRouter, createWebHistory } from 'vue-router';

import HomePage from '../views/HomePage.vue';
import AuctionPage from '../views/AuctionPage.vue';
import MarketPage from '../views/MarketPage.vue';
import ProfilePage from '../views/ProfilePage.vue';

const routes = [
    { path: '/', name: 'home', component: HomePage },
    { path: '/auction', name: 'auction', component: AuctionPage },
    { path: '/market', name: 'market', component: MarketPage },
    { path: '/profile', name: 'profile', component: ProfilePage }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;
