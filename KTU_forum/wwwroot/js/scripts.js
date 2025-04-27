/*!
* Start Bootstrap - Personal v1.0.1 (https://startbootstrap.com/template-overviews/personal)
* Copyright 2013-2023 Start Bootstrap
* Licensed under MIT (https://github.com/StartBootstrap/startbootstrap-personal/blob/master/LICENSE)
*/
// This file is intentionally blank
// Use this file to add JavaScript to your project

document.addEventListener("DOMContentLoaded", function () {
    // Check if the current page is the registration page
    const isRegistrationPage = document.body.classList.contains("registration-page");
    const isLoginPage = document.body.classList.contains("login-page");

    function checkUsernameAvailability() {
        const usernameInput = document.getElementById("Username");
        if (!usernameInput) return;
        const usernameError = document.createElement("div");
        usernameError.style.color = "red";
        usernameInput.parentNode.appendChild(usernameError);

        usernameInput.addEventListener("input", function () {
            let username = usernameInput.value.trim();

            if (username.length > 0) {
                if (isRegistrationPage) {
                    // Handle registration-specific check (Check if username is taken)
                    fetch(`/Registration?handler=CheckUsername&username=${username}`)
                        .then(response => response.json())
                        .then(data => {
                            if (data.isTaken) {
                                usernameError.textContent = "This username is already taken.";
                            } else {
                                usernameError.textContent = "";
                            }
                        })
                        .catch(error => console.error("Error:", error));
                } else if (isLoginPage) {
                    // Handle login-specific check (Check if username exists)
                    fetch(`/Login?handler=CheckUsername&username=${username}`)
                        .then(response => response.json())
                        .then(data => {
                            if (!data.exists) {
                                usernameError.textContent = "This username doesn't exist.";
                            } else {
                                usernameError.textContent = "";
                            }
                        })
                        .catch(error => console.error("Error:", error));
                }
            } else {
                usernameError.textContent = "";
            }
        });
    }

    checkUsernameAvailability();
});

