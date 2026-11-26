export function initShareActions({
	title,
	text,
	url = document.querySelector('[data-share-url]')?.dataset.shareUrl ?? globalThis.location.href,
	copied,
	copyFailed = 'Unable to copy the link',
}) {
	const nativeButton = document.querySelector('[data-share-native]');
	const copyButton = document.querySelector('[data-share-copy]');
	const status = document.querySelector('[data-share-status]');

	if (typeof navigator.share === 'function' && nativeButton) {
		nativeButton.classList.remove('d-none');
		nativeButton.addEventListener('click', async () => {
			try {
				await navigator.share({ title, text, url });
			} catch (error) {
				if (error?.name !== 'AbortError') console.error(error);
			}
		});
	}

	if (navigator.clipboard && copyButton) {
		copyButton.classList.remove('d-none');
		copyButton.addEventListener('click', async () => {
			try {
				await navigator.clipboard.writeText(url);
				if (status) {
					status.textContent = copied;
					status.classList.remove('d-none');
				}
			} catch {
				if (status) {
					status.textContent = copyFailed;
					status.classList.remove('d-none');
				}
			}
		});
	}
}
