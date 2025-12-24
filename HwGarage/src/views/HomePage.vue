<template>
  <section class="home-page">
    <!-- Приветствие -->
    <section class="home-intro">
      <h1 class="home-intro__title">
        Добро пожаловать в HwGarage
      </h1>
      <p class="home-intro__subtitle">
        Управляйте своей коллекцией машинок, участвуйте в аукционах и продавайте модели на торговой площадке.
      </p>
    </section>

    <!-- Блок 1: коллекция -->
    <section class="home-section home-section--collection">
      <div class="home-section__inner">
        <div class="home-section__media">
          <div class="car-card">
            <div class="car-card__image car-card__image--placeholder">
              Фото машинки
            </div>
            <div class="car-card__info">
              <h3 class="car-card__title">Nissan Skyline GT-R R34</h3>
              <p class="car-card__text">
                Лимитированная модель в отличном состоянии. Ваша коллекция может включать
                десятки таких карточек.
              </p>
              <div class="car-card__meta">
                <span class="car-card__meta-item">Статус: в коллекции</span>
                <span class="car-card__meta-item">Добавлена в профиль</span>
              </div>
            </div>
          </div>
        </div>
        <div class="home-section__content">
          <div class="home-section__text-card">
            <h2 class="home-section__title">Ваша коллекция</h2>
            <p class="home-section__text">
              В профиле вы собираете личную коллекцию машинок: добавляете модели, загружаете фотографии
              и указываете описание. Все машинки отображаются в виде аккуратных карточек.
            </p>
            <p class="home-section__text">
              Из коллекции вы сможете выставить машинку на аукцион или разместить объявление на
              торговой площадке в несколько кликов.
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- Блок 2: аукцион -->
    <section class="home-section home-section--auction">
      <div class="home-section__inner">
        <div class="home-section__content">
          <div class="home-section__text-card">
            <h2 class="home-section__title">Аукцион</h2>
            <p class="home-section__text">
              На аукционе вы выставляете машинку с начальной ценой и шагом ставки.
              Пользователи делают ставки, а по завершении аукциона машинка переходит победителю.
            </p>
            <p class="home-section__text">
              В разделе «Аукцион» вы увидите список активных торгов, текущую ставку, время
              окончания и сможете участвовать в торгах.
            </p>
          </div>
        </div>
        <div class="home-section__media">
          <div class="car-card">
            <div class="car-card__image car-card__image--placeholder">
              Фото машинки
            </div>
            <div class="car-card__info">
              <h3 class="car-card__title">Аукцион: Nissan Skyline GT-R R34</h3>
              <p class="car-card__text">
                Текущая ставка: <strong>12 500 ₽</strong><br />
                Шаг ставки: <strong>500 ₽</strong><br />
                До конца: <strong>01:23:45</strong>
              </p>
              <div class="car-card__meta">
                <span class="car-card__meta-item">Статус: активен</span>
                <span class="car-card__meta-item">Сделать ставку →</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Блок 3: торговая площадка -->
    <section class="home-section home-section--market">
      <div class="home-section__inner">
        <div class="home-section__media">
          <div class="car-card">
            <div class="car-card__image car-card__image--placeholder">
              Фото машинки
            </div>
            <div class="car-card__info">
              <h3 class="car-card__title">Toyota Supra MK4</h3>
              <p class="car-card__text">
                Фиксированная цена: <strong>9 900 ₽</strong><br />
                Продавец: <strong>user123</strong>
              </p>
              <div class="car-card__meta">
                <span class="car-card__meta-item">Статус: доступно</span>
                <span class="car-card__meta-item">Купить сразу →</span>
              </div>
            </div>
          </div>
        </div>
        <div class="home-section__content">
          <div class="home-section__text-card">
            <h2 class="home-section__title">Торговая площадка</h2>
            <p class="home-section__text">
              Торговая площадка — это объявления с фиксированной ценой.
              Здесь вы можете сразу купить машинку, не участвуя в аукционе.
            </p>
            <p class="home-section__text">
              В разделе «Торговая площадка» вы увидите активные предложения других пользователей:
              название модели, цену, продавца и текущий статус.
            </p>
          </div>
        </div>
      </div>
    </section>
  </section>
</template>

<script setup>
// логики здесь не требуется, страница статическая
</script>

<style scoped>
/* Ничего не добавляем: полностью используем глобальный style.css */
</style>
