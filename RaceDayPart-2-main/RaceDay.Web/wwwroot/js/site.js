document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.dataset.confirm)) event.preventDefault();
        });
    });

    document.querySelectorAll("form[method='post']").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (event.defaultPrevented) return;
            if (form.dataset.submitting === "true") {
                event.preventDefault();
                return;
            }
            form.dataset.submitting = "true";
            form.querySelectorAll('button[type="submit"],input[type="submit"]').forEach(function (button) {
                button.disabled = true;
                button.dataset.originalText = button.textContent;
                if (button.tagName === "BUTTON") button.textContent = "Please wait…";
            });
        });
    });

    document.querySelectorAll("[data-preview-image]").forEach(function (input) {
        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            var preview = document.getElementById("image-preview");
            if (!preview || !file || !file.type.startsWith("image/")) return;
            var reader = new FileReader();
            reader.onload = function () {
                preview.src = reader.result;
                preview.hidden = false;
            };
            reader.readAsDataURL(file);
        });
    });
});
