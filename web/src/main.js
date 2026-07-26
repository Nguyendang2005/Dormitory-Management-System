// SPA Router and Interactive Client Logic
document.addEventListener("DOMContentLoaded", () => {
    setupRouting();
    setupLoginPresets();
    setupLoginForm();
});

// Routing Controller
function navigateTo(pageId) {
    const pages = document.querySelectorAll(".page-view");
    pages.forEach(p => p.classList.remove("active"));

    const targetPage = document.getElementById(`page-${pageId}`);
    if (targetPage) {
        targetPage.classList.add("active");
        window.scrollTo({ top: 0, behavior: "smooth" });
    }
}

function setupRouting() {
    // Navigation to Login Page
    document.querySelectorAll(".btn-goto-login").forEach(btn => {
        btn.addEventListener("click", (e) => {
            e.preventDefault();
            navigateTo("login");
        });
    });

    // Navigation back to Landing Page
    document.querySelectorAll(".btn-goto-landing").forEach(btn => {
        btn.addEventListener("click", (e) => {
            e.preventDefault();
            navigateTo("landing");
        });
    });

    // Logout Action
    document.querySelectorAll(".btn-logout").forEach(btn => {
        btn.addEventListener("click", () => {
            navigateTo("landing");
        });
    });
}

// Preset Account Handler
function setupLoginPresets() {
    const btnManager = document.getElementById("btn-preset-manager");
    const btnStudent = document.getElementById("btn-preset-student");

    const inputUser = document.getElementById("input-username");
    const inputPass = document.getElementById("input-password");

    if (btnManager) {
        btnManager.addEventListener("click", () => {
            btnManager.classList.add("active");
            if (btnStudent) btnStudent.classList.remove("active");

            inputUser.value = "manager01";
            inputPass.value = "HASH_MANAGER_01";
        });
    }

    if (btnStudent) {
        btnStudent.addEventListener("click", () => {
            btnStudent.classList.add("active");
            if (btnManager) btnManager.classList.remove("active");

            inputUser.value = "student1";
            inputPass.value = "HASH_STUDENT_1";
        });
    }
}

// Login Form Validation & Role Authentication
function setupLoginForm() {
    const form = document.getElementById("form-login");
    const errorMsg = document.getElementById("login-error-msg");

    if (!form) return;

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        if (errorMsg) errorMsg.style.display = "none";

        const username = document.getElementById("input-username").value.trim();
        const password = document.getElementById("input-password").value.trim();

        // Check Manager Role
        if (username === "manager01" || username === "manager02") {
            if (password === "HASH_MANAGER_01" || password === "HASH_MANAGER_02" || password === "123456" || password === "admin123") {
                navigateTo("manager");
                return;
            }
        }

        // Check Student Role
        if (username.startsWith("student")) {
            if (password.startsWith("HASH_STUDENT_") || password === "123456" || password === "student123") {
                const displayName = document.getElementById("student-display-name");
                if (displayName) displayName.textContent = `${username.toUpperCase()} (SE180005)`;
                navigateTo("student");
                return;
            }
        }

        // Invalid Credentials
        if (errorMsg) {
            errorMsg.style.display = "block";
        }
    });
}
