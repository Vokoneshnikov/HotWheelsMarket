<template>
  <main class="page-main market-page">
    <!-- Интро -->
    <section class="market-page__intro">
      <h1 class="market-page__title">
        Торговая площадка
      </h1>
      <p class="market-page__subtitle">
        Покупайте и продавайте модели по фиксированной цене.
      </p>
    </section>

    <!-- Поиск -->
    <section class="market-page__section">
      <form class="market-search" @submit.prevent>
        <div class="market-search__inner">
          <input
              v-model="searchQuery"
              type="text"
              class="market-search__input"
              placeholder="Найдите модель по названию"
          />
          <button
              type="button"
              class="btn primary market-search__button"
              @click="resetSearch"
          >
            Сброс
          </button>
        </div>
      </form>
    </section>

    <!-- Активные объявления + создание -->
    <section class="market-page__section market-section">
      <div class="market-section__header">
        <h2 class="market-section__title">
          Активные объявления ({{ filteredListings.length }})
        </h2>
        <button
            type="button"
            class="btn secondary market-section__action"
            @click="toggleCreate = !toggleCreate"
        >
          {{ toggleCreate ? 'Скрыть форму' : 'Выставить машинку' }}
        </button>
      </div>

      <!-- Форма создания объявления -->
      <div v-if="toggleCreate" class="card" style="margin-bottom: 16px;">
        <h2 class="market-page__title" style="font-size: 22px; text-align: left;">
          Создать объявление
        </h2>
        <p class="market-page__subtitle" style="text-align: left; margin-bottom: 12px;">
          Выберите машинку из своей коллекции и укажите цену продажи.
        </p>

        <form class="form" @submit.prevent="handleCreateListing">
          <label>
            Машинка:
            <select v-model="createForm.carId" required>
              <option value="" disabled>Выберите машинку</option>
              <option
                  v-for="car in availableCars"
                  :key="car.id"
                  :value="car.id"
              >
                {{ car.name }}
              </option>
            </select>
          </label>

          <label>
            Цена (₽):
            <input
                v-model="createForm.price"
                type="number"
                step="0.01"
                min="0"
                required
            />
          </label>

          <button type="submit" class="btn primary" :disabled="creating">
            {{ creating ? 'Создание...' : 'Опубликовать' }}
          </button>
        </form>

        <p v-if="createError" class="form-error">{{ createError }}</p>
        <p v-if="createSuccess" class="form-success">Объявление создано.</p>
      </div>

      <!-- Список объявлений -->
      <section class="market-page__content">
        <p v-if="loading">Загрузка объявлений...</p>

        <ul
            v-else-if="filteredListings.length > 0"
            class="market-list"
        >
          <li
              v-for="listing in filteredListings"
              :key="listing.id"
              class="market-list__item"
          >
            <div class="car-card">
              <div class="car-card__image">
                <img
                    v-if="listing.photoUrl"
                    :src="listing.photoUrl"
                    alt="Фото машинки"
                />
                <div v-else class="car-card__image--placeholder">
                  Фото машинки
                </div>
              </div>

              <div class="car-card__info">
                <h3 class="car-card__title">{{ listing.carName }}</h3>

                <p class="car-card__text">
                  {{ listing.description }}
                </p>

                <div class="car-card__meta">
                  <span class="car-card__meta-item">
                    Цена: {{ listing.price }} ₽
                  </span>
                  <span class="car-card__meta-item">
                    Продавец: {{ listing.sellerName }}
                  </span>
                </div>

                <form class="form" @submit.prevent="handleBuy(listing.id)">
                  <button
                      class="btn primary"
                      type="submit"
                      :disabled="buyingId === listing.id"
                  >
                    {{ buyingId === listing.id ? 'Покупка...' : 'Купить' }}
                  </button>
                </form>
              </div>
            </div>
          </li>
        </ul>

        <p v-else>
          Под подходящий запрос объявления не найдены.
        </p>
      </section>
    </section>
  </main>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';

const API_BASE = 'http://localhost:8080';
const SEARCH_STORAGE_KEY = 'hwgarage_market_search';

const listings = ref([]);
const searchQuery = ref(localStorage.getItem(SEARCH_STORAGE_KEY) || '');
const loading = ref(false);

// отображение формы создания
const toggleCreate = ref(false);

// машины пользователя для селекта
const availableCars = ref([]);

// создание
const createForm = ref({
  carId: '',
  price: ''
});
const creating = ref(false);
const createError = ref('');
const createSuccess = ref(false);

// покупка
const buyingId = ref(null);
const buyError = ref('');

const filteredListings = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  if (!q) return listings.value;
  return listings.value.filter((l) =>
      l.carName.toLowerCase().includes(q)
  );
});

function syncSearchToStorage() {
  try {
    localStorage.setItem(SEARCH_STORAGE_KEY, searchQuery.value);
  } catch {
    // ignore
  }
}

function resetSearch() {
  searchQuery.value = '';
  syncSearchToStorage();
}

async function loadListings() {
  loading.value = true;
  try {
    const res = await fetch(`${API_BASE}/api/market`, {
      credentials: 'include'
    });
    if (res.ok) {
      const data = await res.json();
      listings.value = Array.isArray(data) ? data : [];
      console.log('LISTINGS FROM API:', listings.value);
    } else {
      listings.value = [];
    }
  } catch (e) {
    console.error(e);
    listings.value = [];
  } finally {
    loading.value = false;
  }
}

async function loadAvailableCars() {
  try {
    const res = await fetch(`${API_BASE}/api/my-cars/available`, {
      credentials: 'include'
    });
    if (res.ok) {
      const data = await res.json();
      availableCars.value = Array.isArray(data) ? data : [];
    } else {
      availableCars.value = [];
    }
  } catch (e) {
    console.error(e);
    availableCars.value = [];
  }
}

async function handleCreateListing() {
  createError.value = '';
  createSuccess.value = false;

  creating.value = true;
  try {
    const payload = {
      carId: Number(createForm.value.carId),
      price: Number(createForm.value.price)
    };

    const res = await fetch(`${API_BASE}/api/market/add`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    });

    const result = await res.json().catch(() => ({}));

    if (!res.ok || !result.success) {
      throw new Error(result.error || 'Ошибка при создании объявления');
    }

    createSuccess.value = true;
    createForm.value = { carId: '', price: '' };
    await Promise.all([loadListings(), loadAvailableCars()]);
  } catch (e) {
    console.error(e);
    createError.value = e.message || 'Не удалось создать объявление';
  } finally {
    creating.value = false;
  }
}

async function handleBuy(listingId) {
  buyError.value = '';
  buyingId.value = listingId;
  try {
    const res = await fetch(`${API_BASE}/api/market/buy`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ listingId })
    });

    const result = await res.json().catch(() => ({}));

    if (!res.ok || !result.success) {
      throw new Error(result.error || 'Ошибка покупки');
    }

    await Promise.all([loadListings(), loadAvailableCars()]);
  } catch (e) {
    console.error(e);
    buyError.value = e.message || 'Не удалось купить машинку';
    alert(buyError.value);
  } finally {
    buyingId.value = null;
  }
}

onMounted(async () => {
  await Promise.all([loadListings(), loadAvailableCars()]);
  syncSearchToStorage();
});

watch(searchQuery, () => {
  syncSearchToStorage();
});
</script>

<style scoped>
/* Используем глобальные market-page, market-section, market-list и т.п. */
</style>
