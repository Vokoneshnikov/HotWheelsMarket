<template>
  <div class="page">
    <header class="page-header">
      <a href="/" class="page-header__logo-link">
        <div class="page-header__logo">HwGarage</div>
      </a>

      <nav class="page-header__nav">
        <RouterLink to="/" class="page-header__nav-item">
          <span class="page-header__nav-icon">🏠</span>
          <p class="page-header__nav-label">Главная</p>
        </RouterLink>

        <RouterLink to="/auction" class="page-header__nav-item">
          <span class="page-header__nav-icon">⚡</span>
          <p class="page-header__nav-label">Аукцион</p>
        </RouterLink>

        <RouterLink to="/market" class="page-header__nav-item">
          <span class="page-header__nav-icon">🛒</span>
          <p class="page-header__nav-label">Площадка</p>
        </RouterLink>

        <RouterLink
            to="/profile"
            class="page-header__nav-item page-header__nav-item--primary"
        >
          <span class="page-header__nav-icon">👤</span>
          <p class="page-header__nav-label">Профиль</p>
        </RouterLink>
      </nav>
    </header>

    <div class="page__body">
      <main class="page-main">
        <RouterView />
      </main>
    </div>

    <footer class="page-footer">
      <p class="page-footer__text">
        HwGarage — учебный проект (аукцион, площадка, профиль).
      </p>
    </footer>
  </div>
</template>

<script setup>
import { RouterLink, RouterView } from 'vue-router';
</script>
