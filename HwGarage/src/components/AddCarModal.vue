<template>
  <div
      v-if="visible"
      class="modal-backdrop"
      @click.self="close"
  >
    <div class="modal">
      <h2 class="modal__title">Добавить машинку</h2>

      <form class="modal__form" @submit.prevent="onSubmit">
        <label>
          Название
          <input
              v-model="localCar.name"
              type="text"
              required
          />
        </label>

        <label>
          Описание
          <textarea
              v-model="localCar.description"
              rows="3"
          ></textarea>
        </label>

        <label>
          Фото
          <input
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              @change="onFileChange"
          />
        </label>

<!--        <label>-->
<!--          Статус-->
<!--          <select v-model="localCar.status">-->
<!--            <option value="available">available</option>-->
<!--            <option value="pending">pending</option>-->
<!--          </select>-->
<!--        </label>-->

        <div class="modal__actions">
          <button type="submit" class="btn primary">Сохранить</button>
          <button type="button" class="btn secondary" @click="close">
            Отмена
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, watch } from 'vue';

const props = defineProps({
  visible: {
    type: Boolean,
    required: true
  },
  modelValue: {
    type: Object,
    default: () => ({
      name: '',
      description: '',
      status: 'available'
    })
  }
});

const emit = defineEmits(['update:visible', 'submit']);

const localCar = reactive({ ...props.modelValue });
const fileRef = ref(null);

watch(
    () => props.modelValue,
    (val) => {
      Object.assign(localCar, val || {});
      fileRef.value = null;
    }
);

function close() {
  emit('update:visible', false);
}

function onFileChange(e) {
  const [file] = e.target.files || [];
  fileRef.value = file || null;
}

function onSubmit() {
  const formData = new FormData();
  formData.append('name', localCar.name ?? '');
  formData.append('description', localCar.description ?? '');
  // formData.append('status', localCar.status ?? 'available');

  if (fileRef.value) {
    formData.append('photo', fileRef.value);
  }

  emit('submit', formData);
}
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: #fff;
  border-radius: 10px;
  padding: 16px 18px;
  width: 400px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.modal__title {
  margin: 0 0 8px 0;
}

.modal__form {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.modal__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
