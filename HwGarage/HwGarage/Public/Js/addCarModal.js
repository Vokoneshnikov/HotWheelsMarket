const modal = document.getElementById("addCarModal");
const openBtn = document.getElementById("addCarBtn");
const closeBtn = document.getElementById("closeModal");
const form = modal.querySelector("form");

openBtn.addEventListener("click", () => {
    modal.style.display = "flex";
});

closeBtn.addEventListener("click", () => {
    modal.style.display = "none";
});

form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const name = form.name.value.trim();
    const description = form.description.value.trim();
    const file = form.photo.files[0];
    
    if (!/^[A-Za-zА-Яа-яЁё0-9\- ]{2,100}$/.test(name)) {
        alert("Название должно содержать только английские буквы, цифры, пробелы и дефис (2–100 символов).");
        return;
    }
    
    if (description.length > 500) {
        alert("Описание не может быть длиннее 500 символов.");
        return;
    }
    
    if (!file) {
        alert("Загрузите фотографию.");
        return;
    }

    const allowedExt = [".jpg", ".jpeg", ".png", ".webp"];
    const ext = file.name.toLowerCase().slice(file.name.lastIndexOf("."));

    if (!allowedExt.includes(ext)) {
        alert("Допустимые форматы: JPG, PNG, WEBP.");
        return;
    }

    if (file.size > 5 * 1024 * 1024) {
        alert("Размер файла должен быть не более 5 МБ.");
        return;
    }
    
    let res = await fetch("/cars/add", {
        method: "POST",
        body: new FormData(form)
    });

    if (res.ok) {
        alert("Машинка успешно добавлена!");
        modal.style.display = "none";
        form.reset();
        location.reload();
    } else {
        const text = await res.text();
        alert(text);
    }
});
