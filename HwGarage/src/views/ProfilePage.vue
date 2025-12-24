<template>
  <main class="page-main profile-page">
    <!-- Карточка пользователя -->
    <section class="profile-page__section">
      <div class="card profile-card" v-if="profile">
        <a href="/logout" class="profile-card__logout-link">
          Выйти
        </a>

        <div class="profile-card__avatar">
          <span class="profile-card__avatar-icon">👤</span>
        </div>

        <div class="profile-card__username">
          @{{ profile.username }}
        </div>

        <div class="profile-card__name">
          {{ profile.firstName }} {{ profile.lastName }}
        </div>

        <div class="profile-card__email">
          {{ profile.email }}
        </div>

        <div class="profile-card__balance-row">
          <div class="profile-card__balance">
            <span class="profile-card__balance-label">Баланс:</span>
            <span class="profile-card__balance-value">
              {{ profile.balance }} ₽
            </span>
          </div>

          <div class="profile-page__topup">
            <input
                v-model.number="topUpAmount"
                type="number"
                min="10"
                step="10"
                class="profile-page__amount-input"
                placeholder="Сумма, ₽"
            />
            <button
                class="btn primary profile-card__topup-btn"
                type="button"
                @click="topUpBalance"
            >
              Пополнить
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- История ставок -->
    <section class="profile-page__section profile-section profile-section--bids">
      <h2 class="profile-section__title">История ставок</h2>

      <ul class="profile-bids" v-if="bids.length > 0">
        <li
            v-for="bid in bids"
            :key="bid.auctionId + '-' + bid.createdAt"
            class="card profile-bid"
        >
          <div class="profile-bid__row">
            <span class="profile-bid__title">
              Ставка на аукцион #{{ bid.auctionId }}
            </span>
            <span class="profile-bid__amount">
              {{ bid.amount }} ₽
            </span>
          </div>
          <div class="profile-bid__meta">
            <span class="profile-bid__date">
              {{ bid.createdAt }}
            </span>
          </div>
        </li>
      </ul>

      <p v-else>
        У вас пока нет ставок.
      </p>
    </section>

    <!-- Мои машинки -->
    <section class="profile-page__section profile-section profile-section--cars">
      <div class="profile-section__header">
        <h2 class="profile-section__title">
          Мои машинки ({{ cars.length }})
        </h2>
        <button
            class="btn primary profile-section__action"
            type="button"
            @click="openModal"
        >
          Добавить машинку
        </button>
      </div>

      <ul class="profile-cars" v-if="cars.length > 0">
        <li
            v-for="car in cars"
            :key="car.id"
            class="profile-cars__item"
        >
          <div class="car-card">
            <div class="car-card__image">
              <img
                  v-if="car.photoUrl"
                  :src="car.photoUrl"
                  alt="Фото машинки"
              />
              <div v-else class="car-card__image--placeholder">
                Фото машинки
              </div>
            </div>

            <div class="car-card__info">
              <h3 class="car-card__title">{{ car.name }}</h3>

              <p class="car-card__text">
                {{ car.description }}
              </p>

              <div class="car-card__meta">
                <span class="car-card__meta-item">
                  Статус: {{ car.status }}
                </span>
                <span class="car-card__meta-item">
                  Владелец: вы
                </span>
              </div>
            </div>
          </div>
        </li>
      </ul>

      <p v-else>
        У вас пока нет машинок. Добавьте первую!
      </p>
    </section>

    <!-- Модалка добавления машинки -->
    <AddCarModal
        :visible="isModalOpen"
        :model-value="newCarDraft"
        @update:visible="isModalOpen = $event"
        @submit="handleAddCarSubmit"
    />
  </main>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import AddCarModal from '../components/AddCarModal.vue';

const API_BASE = 'http://localhost:8080';
const CARS_STORAGE_KEY = 'hwgarage_profile_cars';

const cars = ref([]);
const bids = ref([]);
const profile = ref(null);

const isModalOpen = ref(false);
const newCarDraft = ref({
  name: '',
  description: '',
  status: 'available'
});

const topUpAmount = ref(500);

function syncCarsToStorage() {
  try {
    localStorage.setItem(CARS_STORAGE_KEY, JSON.stringify(cars.value));
  } catch {
    // ignore
  }
}

onMounted(async () => {
  const saved = localStorage.getItem(CARS_STORAGE_KEY);
  if (saved) {
    try {
      const parsed = JSON.parse(saved);
      if (Array.isArray(parsed)) {
        cars.value = parsed;
      }
    } catch {
      // ignore
    }
  }

  try {
    const response = await fetch(`${API_BASE}/api/profile`, {
      credentials: 'include'
    });
    if (!response.ok) {
      throw new Error('Failed to load profile');
    }
    const data = await response.json();
    profile.value = data;
    cars.value = data.cars || [];
    bids.value = data.bids || [];
    syncCarsToStorage();
    console.log('PROFILE FROM API:', data);
  } catch (error) {
    console.error(error);
  }
});

function openModal() {
  isModalOpen.value = true;
}

async function handleAddCarSubmit(formData) {
  try {
    const response = await fetch(`${API_BASE}/api/cars`, {
      method: 'POST',
      credentials: 'include',
      body: formData
    });

    const result = await response.json();

    if (!response.ok || !result.success) {
      throw new Error(result.error || 'Ошибка при добавлении машинки');
    }

    const carFromServer = result.car || {};
    const newCar = {
      id: carFromServer.id ?? Date.now(),
      name: carFromServer.name ?? formData.get('name'),
      description: carFromServer.description ?? formData.get('description'),
      status: carFromServer.status ?? formData.get('status') ?? 'pending',
      photoUrl: carFromServer.photoUrl ?? null
    };

    cars.value.push(newCar);
    syncCarsToStorage();

    newCarDraft.value = {
      name: '',
      description: '',
      status: 'available'
    };
    isModalOpen.value = false;
  } catch (error) {
    console.error(error);
    alert(error.message || 'Не удалось добавить машинку');
  }
}

async function topUpBalance() {
  try {
    const amountRub = Number(topUpAmount.value) || 0;
    if (amountRub <= 0) {
      alert('Введите положительную сумму пополнения');
      return;
    }

    const amountMinor = Math.round(amountRub * 100);

    const res = await fetch(`${API_BASE}/api/wallet/create-session`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ amount: amountMinor })
    });

    const result = await res.json().catch(() => ({}));

    if (!res.ok || !result.success || !result.url) {
      throw new Error(result.error || 'Не удалось создать платёжную сессию');
    }

    window.location.href = result.url;
  } catch (e) {
    console.error(e);
    alert(e.message || 'Ошибка при переходе на оплату');
  }
}
</script>

<style scoped>
.profile-page__topup {
  display: flex;
  gap: 8px;
  align-items: center;
}

.profile-page__amount-input {
  width: 120px;
  padding: 6px 10px;
  border-radius: 9999px;
  border: 1px solid #cccccc;
  font-size: 14px;
}
</style>
