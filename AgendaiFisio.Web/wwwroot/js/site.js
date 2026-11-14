(() => {
    "use strict";

    const digits = value => (value || "").replace(/\D/g, "");

    document.querySelectorAll("[data-dismiss-flash]").forEach(button => {
        button.addEventListener("click", () => button.closest(".flash")?.remove());
    });

    document.querySelectorAll("[data-toggle-password]").forEach(button => {
        button.addEventListener("click", () => {
            const input = button.closest(".input-wrap")?.querySelector("[data-password-input]");
            if (!input) return;
            const show = input.type === "password";
            input.type = show ? "text" : "password";
            button.querySelector(".material-symbols-rounded").textContent = show ? "visibility_off" : "visibility";
            button.setAttribute("aria-label", show ? "Ocultar senha" : "Mostrar senha");
        });
    });

    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            if (!window.confirm(form.dataset.confirm || "Confirma esta ação?")) event.preventDefault();
        });
    });

    document.querySelectorAll("form").forEach(form => {
        form.addEventListener("submit", () => {
            if (!form.checkValidity()) return;
            const button = form.querySelector("button[type='submit']");
            if (!button || button.dataset.keepEnabled !== undefined) return;
            window.setTimeout(() => {
                button.disabled = true;
                button.setAttribute("aria-busy", "true");
            }, 0);
        });
    });

    document.querySelectorAll("[data-mask]").forEach(input => {
        const format = () => {
            const value = digits(input.value);
            switch (input.dataset.mask) {
                case "cpf":
                    input.value = value.slice(0, 11)
                        .replace(/(\d{3})(\d)/, "$1.$2")
                        .replace(/(\d{3})(\d)/, "$1.$2")
                        .replace(/(\d{3})(\d{1,2})$/, "$1-$2");
                    break;
                case "phone":
                    input.value = value.slice(0, 11)
                        .replace(/^(\d{2})(\d)/, "($1) $2")
                        .replace(/(\d{5})(\d)/, "$1-$2");
                    break;
                case "cep":
                    input.value = value.slice(0, 8).replace(/(\d{5})(\d)/, "$1-$2");
                    break;
            }
        };
        input.addEventListener("input", format);
        format();
    });

    const registerForm = document.querySelector("[data-register-form]");
    if (registerForm) {
        const patientArea = registerForm.querySelector("[data-patient-registration]");
        const guardianArea = registerForm.querySelector("[data-guardian-fields]");
        const birthDate = registerForm.querySelector("[name='BirthDate']");
        const userTypes = registerForm.querySelectorAll("[name='UserType']");

        const updateRegistration = () => {
            const isPatient = registerForm.querySelector("[name='UserType']:checked")?.value === "Paciente";
            patientArea.hidden = !isPatient;
            birthDate.required = isPatient;
            patientArea.querySelectorAll("input").forEach(input => input.disabled = !isPatient);

            if (!isPatient || !birthDate.value) {
                guardianArea.hidden = true;
                guardianArea.querySelectorAll("input").forEach(input => input.disabled = true);
                return;
            }

            const birth = new Date(`${birthDate.value}T12:00:00`);
            const today = new Date();
            let age = today.getFullYear() - birth.getFullYear();
            const beforeBirthday = today.getMonth() < birth.getMonth() || (today.getMonth() === birth.getMonth() && today.getDate() < birth.getDate());
            if (beforeBirthday) age--;
            guardianArea.hidden = age >= 16;
            guardianArea.querySelectorAll("input").forEach(input => input.disabled = age >= 16);
        };

        userTypes.forEach(input => input.addEventListener("change", updateRegistration));
        birthDate.addEventListener("change", updateRegistration);
        updateRegistration();
    }

    const termsDialog = document.querySelector("[data-terms-dialog]");
    document.querySelectorAll("[data-open-terms]").forEach(link => {
        link.addEventListener("click", event => {
            event.preventDefault();
            termsDialog?.showModal();
        });
    });
    document.querySelectorAll("[data-close-dialog]").forEach(button => {
        button.addEventListener("click", () => termsDialog?.close());
    });
    termsDialog?.addEventListener("click", event => {
        if (event.target === termsDialog) termsDialog.close();
    });

    document.querySelectorAll("[data-address-form]").forEach(form => {
        const cepInput = form.querySelector("[data-cep-input]");
        const spinner = form.querySelector("[data-cep-spinner]");
        const feedback = form.querySelector("[data-cep-feedback]");
        if (!cepInput) return;

        let lastCep = "";
        const searchCep = async () => {
            const cep = digits(cepInput.value);
            if (cep.length !== 8 || cep === lastCep) return;
            lastCep = cep;
            spinner.hidden = false;
            feedback.textContent = "Buscando endereço…";

            try {
                const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`, { headers: { "Accept": "application/json" } });
                if (!response.ok) throw new Error();
                const address = await response.json();
                if (address.erro) throw new Error();

                form.querySelector("[data-address-street]").value = address.logradouro || "";
                form.querySelector("[data-address-neighborhood]").value = address.bairro || "";
                form.querySelector("[data-address-city]").value = address.localidade || "";
                form.querySelector("[data-address-state]").value = address.uf || "";
                feedback.textContent = "Endereço encontrado.";
                feedback.style.color = "#15803d";
            } catch {
                feedback.textContent = "CEP não encontrado. Preencha o endereço manualmente.";
                feedback.style.color = "#b45309";
                lastCep = "";
            } finally {
                spinner.hidden = true;
            }
        };

        cepInput.addEventListener("blur", searchCep);
        cepInput.addEventListener("input", () => {
            if (digits(cepInput.value).length === 8) searchCep();
        });
    });

    document.querySelectorAll("[data-appointment-form]").forEach(form => {
        const professional = form.querySelector("[data-professional-select]");
        const dateInput = form.querySelector("[data-appointment-date]");
        const feedback = form.querySelector("[data-availability-feedback]");
        const submit = form.querySelector("button[type='submit']");
        const template = form.dataset.unavailabilityUrl;
        let requestController;

        const showFeedback = (message, color = "") => {
            feedback.textContent = message;
            feedback.style.color = color;
        };

        const checkAvailability = async () => {
            if (!professional.value || !dateInput.value) {
                submit.disabled = false;
                showFeedback("Escolha um profissional e um horário para consultar a disponibilidade.");
                return;
            }

            const selected = new Date(dateInput.value);
            if (Number.isNaN(selected.getTime())) return;

            requestController?.abort();
            requestController = new AbortController();
            submit.disabled = true;
            showFeedback("Consultando a agenda…");

            const start = new Date(selected);
            start.setHours(0, 0, 0, 0);
            const end = new Date(start);
            end.setDate(end.getDate() + 1);
            const asLocalIso = date => {
                const pad = number => String(number).padStart(2, "0");
                return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
            };
            const path = template.replace("00000000-0000-0000-0000-000000000000", professional.value);
            const url = new URL(path, window.location.origin);
            url.searchParams.set("start", asLocalIso(start));
            url.searchParams.set("end", asLocalIso(end));

            try {
                const response = await fetch(url, { signal: requestController.signal, headers: { "Accept": "application/json" } });
                if (!response.ok) throw new Error();
                const blocks = await response.json();
                const unavailable = blocks.some(block => {
                    const blockStart = new Date(block.inicio);
                    const blockEnd = new Date(block.fim);
                    return selected >= blockStart && selected < blockEnd;
                });

                submit.disabled = unavailable;
                showFeedback(
                    unavailable ? "Este horário está indisponível. Escolha outro." : "Horário disponível para solicitação.",
                    unavailable ? "#b91c1c" : "#15803d"
                );
            } catch (error) {
                if (error.name === "AbortError") return;
                submit.disabled = false;
                showFeedback("Não foi possível consultar agora. O servidor fará a validação ao confirmar.", "#b45309");
            }
        };

        professional.addEventListener("change", checkAvailability);
        dateInput.addEventListener("change", checkAvailability);
        if (professional.value && dateInput.value) checkAvailability();
    });

    document.querySelectorAll("[data-photo-form]").forEach(form => {
        const input = form.querySelector("[data-photo-input]");
        const submit = form.querySelector("[data-photo-submit]");
        input?.addEventListener("change", () => {
            if (!input.files?.length) {
                submit.hidden = true;
                return;
            }

            const file = input.files[0];
            const validTypes = ["image/jpeg", "image/png", "image/webp"];
            if (!validTypes.includes(file.type) || file.size > 5 * 1024 * 1024) {
                window.alert("Escolha uma imagem JPEG, PNG ou WebP com no máximo 5 MB.");
                input.value = "";
                submit.hidden = true;
                return;
            }

            submit.hidden = false;
            submit.textContent = `Enviar ${file.name}`;
        });
    });
})();
