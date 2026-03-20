window.qrScanner = {
    _stream: null,
    _interval: null,

    start: async function (dotnetRef, videoId, canvasId) {
        try {
            this._stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: { ideal: 'environment' } }
            });

            const video = document.getElementById(videoId);
            if (!video) {
                console.error('video element not found:', videoId);
                return false;
            }

            video.srcObject = this._stream;

            await new Promise((resolve, reject) => {
                video.onloadedmetadata = () => {
                    video.play().then(resolve).catch(reject);
                };
            });

            const canvas = document.getElementById(canvasId);
            const ctx = canvas.getContext('2d');

            this._interval = setInterval(() => {
                if (video.readyState !== video.HAVE_ENOUGH_DATA) return;

                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

                const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const code = jsQR(imageData.data, imageData.width, imageData.height);

                if (code && code.data) {
                    dotnetRef.invokeMethodAsync('OnQrDetected', code.data);
                }
            }, 300);

            return true;
        } catch (e) {
            console.error('Camera error:', e);
            if (this._stream) {
                this._stream.getTracks().forEach(t => t.stop());
                this._stream = null;
            }
            return false;
        }
    },

    stop: function () {
        if (this._interval) {
            clearInterval(this._interval);
            this._interval = null;
        }
        if (this._stream) {
            this._stream.getTracks().forEach(t => t.stop());
            this._stream = null;
        }
    }
};
