/*!
* Start Bootstrap - Personal v1.0.1 (https://startbootstrap.com/template-overviews/personal)
* Copyright 2013-2023 Start Bootstrap
* Licensed under MIT (https://github.com/StartBootstrap/startbootstrap-personal/blob/master/LICENSE)
*/
// This file is intentionally blank
// Use this file to add JavaScript to your project

document.addEventListener("DOMContentLoaded", function () {
    function checkUsernameAvailability() {
        const usernameInput = document.getElementById("Username");
        const usernameError = document.createElement("div");
        usernameError.style.color = "red";
        usernameInput.parentNode.appendChild(usernameError);

        usernameInput.addEventListener("input", function () {
            let username = usernameInput.value.trim();

            if (username.length > 0) {
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
            } else {
                usernameError.textContent = "";
            }
        });
    }

    checkUsernameAvailability();
});
