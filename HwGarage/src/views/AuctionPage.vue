<template>
  <main class="page-main auction-page">
    <!-- Интро -->
    <section class="auction-page__intro">
      <h1 class="auction-page__title">
        Аукцион
      </h1>
      <p class="auction-page__subtitle">
        Ищите интересующие модели и участвуйте в торгах в один клик.
      </p>
    </section>

    <!-- Поиск -->
    <section class="auction-page__section">
      <form class="auction-search" @submit.prevent>
        <div class="auction-search__inner">
          <input
              v-model="searchQuery"
              type="text"
              class="auction-search__input"
              placeholder="Найдите аукцион по названию машинки"
          />
          <button
              type="button"
              class="btn primary auction-search__button"
              @click="resetSearch"
          >
            Сброс
          </button>
        </div>
      </form>
    </section>

    <!-- Секция с аукционами + создание -->
    <section class="auction-page__section auction-section">
      <div class="auction-section__header">
        <h2 class="auction-section__title">
          Активные аукционы ({{ filteredAuctions.length }})
        </h2>
        <button
            type="button"
            class="btn secondary auction-section__action"
            @click="toggleCreate = !toggleCreate"
        >
          {{ toggleCreate ? 'Скрыть форму' : 'Создать аукцион' }}
        </button>
      </div>

      <!-- Форма создания аукциона в карточке -->
      <div v-if="toggleCreate" class="card" style="margin-bottom: 16px;">
        <h2
            class="home-intro__title"
            style="font-size: 22px; text-align: left; margin-bottom: 8px;"
        >
          Создать аукцион
        </h2>
        <p
            class="home-intro__subtitle"
            style="text-align: left; margin-bottom: 12px;"
        >
          Выберите машинку из вашей коллекции, начальную цену, шаг ставки и время окончания.
        </p>

        <form class="form" @submit.prevent="handleCreateAuction">
          <label>
            Машинка
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
            Стартовая цена (RUB)
            <input
                v-model="createForm.startPrice"
                type="number"
                step="0.01"
                min="0"
                required
            />
          </label>

          <label>
            Шаг ставки (RUB)
            <input
                v-model="createForm.bidStep"
                type="number"
                step="0.01"
                min="0.01"
                required
            />
          </label>

          <label>
            Время окончания
            <input
                v-model="createForm.endsAt"
                type="datetime-local"
                required
            />
          </label>

          <button class="btn primary" type="submit" :disabled="creating">
            {{ creating ? 'Создание...' : 'Создать аукцион' }}
          </button>
        </form>

        <p v-if="createError" class="form-error">{{ createError }}</p>
        <p v-if="createSuccess" class="form-success">Аукцион создан.</p>
      </div>

      <!-- Список аукционов -->
      <p v-if="loading">Загрузка аукционов...</p>

      <ul
          v-else-if="filteredAuctions.length > 0"
          class="auction-list"
      >
        <li
            v-for="auction in filteredAuctions"
            :key="auction.id"
            class="auction-list__item"
        >
          <div class="car-card">
            <div class="car-card__image">
              <img
                  v-if="auction.photoUrl"
                  :src="auction.photoUrl"
                  alt="Фото машинки"
              />
              <div v-else class="car-card__image--placeholder">
                Фото машинки
              </div>
            </div>

            <div class="car-card__info">
              <h3 class="car-card__title">{{ auction.carName }}</h3>

              <p class="car-card__text">
                {{ auction.description }}
              </p>

              <div class="car-card__meta">
                <span class="car-card__meta-item">
                  Текущая ставка: {{ auction.currentBid }} ₽
                </span>
                <span class="car-card__meta-item">
                  Шаг: {{ auction.bidStep }} ₽
                </span>
                <span class="car-card__meta-item">
                  До: {{ auction.endsAt }}
                </span>
              </div>

              <div>
                <a
                    :href="`/auction/view?id=${auction.id}`"
                    class="btn primary"
                >
                  Открыть аукцион →
                </a>
              </div>
            </div>
          </div>
        </li>
      </ul>

      <p v-else>
        Под подходящий запрос аукционы не найдены.
      </p>
    </section>
  </main>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';

const API_BASE = 'http://localhost:8080';
const SEARCH_STORAGE_KEY = 'hwgarage_auction_search';

const auctions = ref([]);
const searchQuery = ref(localStorage.getItem(SEARCH_STORAGE_KEY) || '');
const loading = ref(false);

// отображение формы создания
const toggleCreate = ref(false);

// доступные машинки пользователя
const availableCars = ref([]);

// форма создания
const createForm = ref({
  carId: '',
  startPrice: '',
  bidStep: '',
  endsAt: ''
});
const creating = ref(false);
const createError = ref('');
const createSuccess = ref(false);

const filteredAuctions = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  if (!q) return auctions.value;
  return auctions.value.filter((a) =>
      a.carName.toLowerCase().includes(q)
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

async function loadAuctions() {
  loading.value = true;
  try {
    const res = await fetch(`${API_BASE}/api/auctions`, {
      credentials: 'include'
    });
    if (res.ok) {
      const data = await res.json();
      auctions.value = Array.isArray(data) ? data : [];
      console.log('AUCTIONS FROM API:', auctions.value);
    } else {
      auctions.value = [];
    }
  } catch (e) {
    console.error(e);
    auctions.value = [];
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

async function handleCreateAuction() {
  createError.value = '';
  createSuccess.value = false;

  creating.value = true;
  try {
    const payload = {
      carId: Number(createForm.value.carId),
      startPrice: Number(createForm.value.startPrice),
      bidStep: Number(createForm.value.bidStep),
      endsAt: createForm.value.endsAt
    };

    const res = await fetch(`${API_BASE}/api/auctions`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    });

    const result = await res.json().catch(() => ({}));

    if (!res.ok || !result.success) {
      throw new Error(result.error || 'Ошибка при создании аукциона');
    }

    createSuccess.value = true;

    createForm.value = {
      carId: '',
      startPrice: '',
      bidStep: '',
      endsAt: ''
    };

    await Promise.all([loadAuctions(), loadAvailableCars()]);
  } catch (e) {
    console.error(e);
    createError.value = e.message || 'Не удалось создать аукцион';
  } finally {
    creating.value = false;
  }
}

onMounted(async () => {
  await Promise.all([loadAuctions(), loadAvailableCars()]);
  syncSearchToStorage();
});

watch(searchQuery, () => {
  syncSearchToStorage();
});
</script>

<style scoped>
/* Используем глобальные стили auction-page, auction-section, auction-list и т.п. */
</style>
