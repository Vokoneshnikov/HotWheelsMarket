<template>
  <article class="car-card">
    <header class="car-card__header">
      <h3 class="car-card__title">{{ car.name }}</h3>
      <span class="car-card__status">Статус: {{ car.status }}</span>
    </header>

    <p class="car-card__description" v-if="car.description">
      {{ car.description }}
    </p>

    <footer class="car-card__footer">
      <span class="car-card__owner">Владелец: вы</span>
<!--      <slot name="actions"></slot>-->
    </footer>
  </article>
</template>

<script setup>
const props = defineProps({
  car: {
    type: Object,
    required: true
  }
});
</script>

<style scoped>
.car-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #f7f7f7;
}

.car-card__header {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.car-card__title {
  margin: 0;
  font-weight: 600;
}

.car-card__status {
  font-size: 12px;
  color: #555;
}

.car-card__description {
  margin: 0;
  font-size: 14px;
}

.car-card__footer {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #666;
}
</style>
