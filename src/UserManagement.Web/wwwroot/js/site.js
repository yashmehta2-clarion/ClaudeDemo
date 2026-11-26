// Auto-dismiss success alerts after a few seconds.
document.querySelectorAll('.alert-success.alert-dismissible').forEach(el => {
    setTimeout(() => bootstrap.Alert.getOrCreateInstance(el).close(), 5000);
});
