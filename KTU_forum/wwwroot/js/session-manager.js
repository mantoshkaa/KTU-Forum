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
    form.action = '/Login?handler=Logout';  // Use `?handler=Logout` to call the OnPostLogout handler


    // Append the form to the body and submit it
    document.body.appendChild(form);
    form.submit(); // This triggers the logout action
}



