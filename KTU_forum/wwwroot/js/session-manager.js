const sessionDuration = 2 * 60 * 1000; // 30 minutes
const warningTime = 1 * 60 * 1000;     // Show popup at 25 mins (5 mins left)

// Timer to trigger Bootstrap modal popup
setTimeout(() => {
    const modal = new bootstrap.Modal(document.getElementById('sessionTimeoutModal'));
    modal.show();
}, warningTime);


function extendSession() {
    fetch('/KeepAlive')  // Send a request to the /KeepAlive endpoint
        .then(() => {
            // Optionally refresh the page to reset everything
            location.reload();
        })
        .catch((error) => {
            console.error('Error extending session:', error);
        });
}

function endSession() {
    // Create a form dynamically
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Login?handler=Logout';  // The correct handler for logout

    // Optionally add an anti-forgery token if needed
    const tokenInput = document.createElement('input');
    tokenInput.type = 'hidden';
    tokenInput.name = '__RequestVerificationToken';
    tokenInput.value = document.querySelector('input[name="__RequestVerificationToken"]').value;  // Grab token from the page
    form.appendChild(tokenInput);

    // Append the form to the body and submit it
    document.body.appendChild(form);
    form.submit(); // This triggers the OnPostLogout action
}




