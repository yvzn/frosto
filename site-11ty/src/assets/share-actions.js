const shareUrl = document.querySelector('[data-share-url]');
const url = shareUrl ? shareUrl.getAttribute('data-share-url') : window.location.origin + '/';
const nativeButton = document.querySelector('[data-native-share]');
const copyButton = document.querySelector('[data-copy-share]');
const copyStatus = document.querySelector('[data-copy-status]');
const socialLinks = document.querySelector('[data-share-social]');

const title = document.querySelector('meta[property="og:title"]')?.content || document.title;
const text = document.querySelector('meta[property="og:description"]')?.content || '';

if (typeof navigator.share === 'function' && nativeButton) {
	nativeButton.hidden = false;
	if (socialLinks) {
		socialLinks.hidden = true;
	}
	nativeButton.addEventListener('click', async () => {
		try {
			await navigator.share({ title, text, url });
		} catch (error) {
			if (error?.name !== 'AbortError') {
				return;
			}
		}
	});
} else if (socialLinks) {
	socialLinks.hidden = false;
}

if (copyButton) {
	copyButton.addEventListener('click', async () => {
		try {
			await navigator.clipboard.writeText(url);
		} catch {
			const input = document.createElement('textarea');
			input.value = url;
			input.setAttribute('readonly', '');
			input.style.position = 'fixed';
			input.style.opacity = '0';
			document.body.appendChild(input);
			input.select();
			document.execCommand('copy');
			input.remove();
		}
		if (copyStatus) {
			copyStatus.hidden = false;
		}
	});
}
