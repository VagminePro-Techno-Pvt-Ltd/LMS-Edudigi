
(() => {
    "use strict";

    function initialize() {
        const $ = id => document.getElementById(id);

        // Compatible with older and newer Bootstrap Modal versions.
        function modalInstance(element) {
            if (!element || !window.bootstrap?.Modal) {
                return null;
            }

            const Modal = window.bootstrap.Modal;

            try {
                if (typeof Modal.getOrCreateInstance === "function") {
                    return Modal.getOrCreateInstance(element);
                }

                if (typeof Modal.getInstance === "function") {
                    const existing = Modal.getInstance(element);
                    if (existing) {
                        return existing;
                    }
                }

                if (typeof Modal === "function") {
                    return new Modal(element);
                }
            } catch (error) {
                console.error("Internal Assessment modal error:", error);
            }

            return null;
        }

        const statusElement = $("iaStatusModal");
        const confirmElement = $("iaConfirmModal");
        const confirmTitle = $("iaConfirmTitle");
        const confirmMessage = $("iaConfirmMessage");
        const confirmProceed = $("iaConfirmProceed");

        // Show messages returned by the controller.
        if (statusElement) {
            const showStatus = () => {
                const instance = modalInstance(statusElement);

                if (instance) {
                    instance.show();
                    return;
                }

                const message = statusElement
                    .querySelector(".modal-body")
                    ?.textContent?.trim();

                if (message) {
                    window.alert(message);
                }
            };

            if (document.readyState === "complete") {
                showStatus();
            } else {
                window.addEventListener("load", showStatus, {
                    once: true
                });
            }
        }

        // Confirmation dialogs for Publish / Unpublish.
        let pendingForm = null;

        function showMessage(title, message) {
            const instance = modalInstance(confirmElement);

            if (!instance) {
                window.alert(message);
                return;
            }

            pendingForm = null;

            if (confirmTitle) {
                confirmTitle.textContent = title;
            }

            if (confirmMessage) {
                confirmMessage.textContent = message;
            }

            if (confirmProceed) {
                confirmProceed.hidden = true;
            }

            instance.show();
        }

        if (confirmElement) {
            confirmElement.addEventListener(
                "hidden.bs.modal",
                () => {
                    pendingForm = null;

                    if (confirmProceed) {
                        confirmProceed.hidden = false;
                    }
                }
            );
        }

        function submitConfirmedForm(form) {
            if (!form) {
                return;
            }

            form.dataset.confirmed = "true";

            if (typeof form.requestSubmit === "function") {
                form.requestSubmit();
            } else {
                HTMLFormElement.prototype.submit.call(form);
            }
        }

        document
            .querySelectorAll(".ia-confirm-form")
            .forEach(form => {
                form.addEventListener("submit", event => {
                    if (form.dataset.confirmed === "true") {
                        return;
                    }

                    event.preventDefault();

                    const title =
                        form.dataset.confirmTitle ||
                        "Confirm Action";

                    const message =
                        form.dataset.confirmMessage ||
                        "Are you sure you want to continue?";

                    const instance = modalInstance(confirmElement);

                    // Native confirmation fallback.
                    if (!instance) {
                        if (window.confirm(message)) {
                            submitConfirmedForm(form);
                        }

                        return;
                    }

                    pendingForm = form;

                    if (confirmTitle) {
                        confirmTitle.textContent = title;
                    }

                    if (confirmMessage) {
                        confirmMessage.textContent = message;
                    }

                    if (confirmProceed) {
                        confirmProceed.hidden = false;
                    }

                    instance.show();
                });
            });

        confirmProceed?.addEventListener("click", () => {
            if (!pendingForm) {
                return;
            }

            const form = pendingForm;
            pendingForm = null;

            // Submit before closing the modal so the hidden
            // event cannot clear the pending submission.
            submitConfirmedForm(form);

            modalInstance(confirmElement)?.hide();
        });

        // Assessment type multi-select dropdown.
        const typeCheckboxes = Array.from(
            document.querySelectorAll(".ia-type")
        );

        const typeButton = $("iaTypesButton");

        function updateTypes() {
            if (!typeButton) {
                return;
            }

            const selected = typeCheckboxes
                .filter(input => input.checked)
                .map(input => input.value);

            const label = selected.length
                ? selected.join(", ")
                : "Select assessment types";

            typeButton.textContent = label;
            typeButton.title = label;

            typeButton.setAttribute(
                "aria-label",
                selected.length
                    ? "Selected assessment types: " + label
                    : label
            );
        }

        typeCheckboxes.forEach(input => {
            input.addEventListener("change", updateTypes);
        });

        $("iaTypesAll")?.addEventListener("click", () => {
            typeCheckboxes.forEach(input => {
                input.checked = true;
            });

            updateTypes();
        });

        $("iaTypesClear")?.addEventListener("click", () => {
            typeCheckboxes.forEach(input => {
                input.checked = false;
            });

            updateTypes();
        });

        updateTypes();

        // Faculty course / subject / schedule filtering.
        const course = $("iaCourse");
        const subject = $("iaSubject");
        const windowSelect = $("iaWindow");
        const dueDate = $("iaDueDate");
        const dueHelp = $("iaDueHelp");
        const useEnd = $("iaUseEnd");

        function localDateTime() {
            const date = new Date();
            const pad = n => String(n).padStart(2, "0");

            return (
                date.getFullYear() + "-" +
                pad(date.getMonth() + 1) + "-" +
                pad(date.getDate()) + "T" +
                pad(date.getHours()) + ":" +
                pad(date.getMinutes())
            );
        }

        function refreshDueDate() {
            if (!windowSelect || !dueDate) {
                return;
            }

            const option = windowSelect.selectedOptions[0];

            const start = option?.dataset.start || "";
            const end = option?.dataset.end || "";
            const now = localDateTime();

            dueDate.min = start
                ? (start > now ? start : now)
                : "";

            dueDate.max = end;

            if (useEnd) {
                useEnd.disabled =
                    !end || end < dueDate.min;
            }

            if (dueHelp) {
                dueHelp.textContent = end
                    ? "Allowed: " +
                      start.replace("T", " ") +
                      " to " +
                      end.replace("T", " ")
                    : "Select a schedule to see its date range.";
            }

            if (!end) {
                dueDate.value = "";
                return;
            }

            if (
                !dueDate.value ||
                dueDate.value < dueDate.min ||
                dueDate.value > end
            ) {
                dueDate.value =
                    end >= dueDate.min ? end : "";
            }
        }

        function refreshCourse() {
            if (!course) {
                return;
            }

            const selectedCourse = course.value;

            const semester =
                course.selectedOptions[0]?.dataset.semester;

            [subject, windowSelect].forEach(select => {
                if (!select) {
                    return;
                }

                Array.from(select.options).forEach(option => {
                    if (!option.value) {
                        return;
                    }

                    const wrongSemester =
                        !semester ||
                        option.dataset.semester !== semester;

                    const wrongCourse =
                        select === windowSelect &&
                        Boolean(option.dataset.course) &&
                        option.dataset.course !== selectedCourse;

                    option.hidden = wrongSemester || wrongCourse;
                    option.disabled = wrongSemester || wrongCourse;
                });

                select.value = "";
            });

            refreshDueDate();
        }

        // Admin: filter courses by selected policy semester.
        const adminPolicy = $("iaAdminPolicy");
        const adminCourse = $("iaAdminCourse");

        function refreshAdminCourses() {
            if (!adminPolicy || !adminCourse) {
                return;
            }

            const semester =
                adminPolicy.selectedOptions[0]?.dataset.semester;

            Array.from(adminCourse.options).forEach(option => {
                if (!option.value) {
                    return;
                }

                const invalid =
                    !semester ||
                    option.dataset.semester !== semester;

                option.hidden = invalid;
                option.disabled = invalid;
            });

            adminCourse.value = "";
        }

        adminPolicy?.addEventListener(
            "change",
            refreshAdminCourses
        );

        refreshAdminCourses();

        course?.addEventListener("change", refreshCourse);

        windowSelect?.addEventListener(
            "change",
            refreshDueDate
        );

        useEnd?.addEventListener("click", () => {
            if (!dueDate || !windowSelect) {
                return;
            }

            const end =
                windowSelect.selectedOptions[0]?.dataset.end;

            if (
                end &&
                (!dueDate.min || end >= dueDate.min)
            ) {
                dueDate.value = end;
            }
        });

        if (course) {
            refreshCourse();
        }

        // Validate assessment creation.
        const createForm = $("iaCreateForm");

        createForm?.addEventListener("submit", event => {
            const selected = typeCheckboxes.filter(
                input => input.checked
            );

            if (selected.length === 0) {
                event.preventDefault();

                showMessage(
                    "Assessment Type Required",
                    "Please select at least one assessment type."
                );

                return;
            }

            if (dueDate) {
                const invalid =
                    !dueDate.value ||
                    (dueDate.min &&
                        dueDate.value < dueDate.min) ||
                    (dueDate.max &&
                        dueDate.value > dueDate.max);

                if (invalid) {
                    event.preventDefault();

                    showMessage(
                        "Invalid Due Date",
                        "Please select a due date within the approved assessment schedule."
                    );
                }
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initialize,
            { once: true }
        );
    } else {
        initialize();
    }
})();
