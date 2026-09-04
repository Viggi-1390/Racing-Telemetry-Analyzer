/**
 * ROLLING VIEWPORT TELEMETRY GRAPH ENGINE (HTML5 CANVAS)
 * - Pinned Playback Scrubber (80% from left)
 * - Viewport Culling / Virtualization (only draws visible slice)
 * - Progressive Reveal: Data enters from right, scrolls off left
 * - 60 FPS requestAnimationFrame animation loop
 */

(function () {
    let canvas = null;
    let ctx = null;
    let telemetryData = [];
    let dotNetHelper = null;

    let isPlaying = false;
    let playbackRate = 1;
    let windowDistance = 1000.0; // 1.0 km rolling window
    let currentIndex = 0;
    let lastFrameTime = 0;
    let animFrameId = null;

    // Pinned scrubber X ratio (0.80 = 80% across canvas, current position indicator)
    const SCRUBBER_X_RATIO = 0.80;

    window.initRollingTelemetryCanvas = function (dotNetRef, data) {
        dotNetHelper = dotNetRef;
        telemetryData = data || [];
        canvas = document.getElementById('rollingTelemetryCanvas');
        if (!canvas) return;
        ctx = canvas.getContext('2d');

        // Set high-DPI scaling
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);

        currentIndex = 0;
        isPlaying = false;
        renderFrame();
    };

    function resizeCanvas() {
        if (!canvas) return;
        const rect = canvas.getBoundingClientRect();
        canvas.width = rect.width * window.devicePixelRatio;
        canvas.height = rect.height * window.devicePixelRatio;
        ctx.scale(window.devicePixelRatio, window.devicePixelRatio);
    }

    window.setRollingPlaybackState = function (playing, rate, windowDist) {
        isPlaying = playing;
        playbackRate = rate || 1;
        windowDistance = windowDist || 1000.0;

        if (isPlaying) {
            lastFrameTime = performance.now();
            if (!animFrameId) animLoop();
        } else {
            if (animFrameId) {
                cancelAnimationFrame(animFrameId);
                animFrameId = null;
            }
            renderFrame();
        }
    };

    window.seekRollingPlayback = function (index) {
        currentIndex = Math.max(0, Math.min(telemetryData.length - 1, index));
        renderFrame();
    };

    window.resetRollingPlayback = function () {
        isPlaying = false;
        currentIndex = 0;
        if (animFrameId) {
            cancelAnimationFrame(animFrameId);
            animFrameId = null;
        }
        renderFrame();
    };

    function animLoop(now) {
        if (!isPlaying) return;

        if (!now) now = performance.now();
        const dt = (now - lastFrameTime) / 1000.0;
        lastFrameTime = now;

        // Advance index based on playback rate and timestamp progression
        const step = Math.max(1, Math.round(playbackRate * (dt * 60)));
        currentIndex += step;

        if (currentIndex >= telemetryData.length - 1) {
            currentIndex = telemetryData.length - 1;
            isPlaying = false;
            if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnPlaybackTick', currentIndex);
            renderFrame();
            return;
        }

        if (dotNetHelper && currentIndex % 3 === 0) {
            dotNetHelper.invokeMethodAsync('OnPlaybackTick', currentIndex);
        }

        renderFrame();
        animFrameId = requestAnimationFrame(animLoop);
    }

    function renderFrame() {
        if (!canvas || !ctx || telemetryData.length === 0) return;

        const width = canvas.width / window.devicePixelRatio;
        const height = canvas.height / window.devicePixelRatio;

        // Clear background
        ctx.fillStyle = '#08080c';
        ctx.fillRect(0, 0, width, height);

        const currentPt = telemetryData[currentIndex];
        const currentDist = currentPt.distance;

        // Calculate visible rolling window range in distance
        // Scrubber is pinned at SCRUBBER_X_RATIO (80% from left)
        const windowStartDist = currentDist - (windowDistance * SCRUBBER_X_RATIO);
        const windowEndDist = windowStartDist + windowDistance;

        // 1. Draw Grid Lines & X-Axis (Distance in km)
        drawGrid(width, height, windowStartDist, windowEndDist);

        // 2. Viewport Virtualization / Culling: Find visible slice of points
        const startIndex = binarySearchLower(telemetryData, windowStartDist);
        const endIndex = binarySearchUpper(telemetryData, windowEndDist);

        if (startIndex <= endIndex && startIndex < telemetryData.length) {
            const visibleSlice = telemetryData.slice(startIndex, endIndex + 1);

            // Channel line coordinate mapping
            const getX = (dist) => ((dist - windowStartDist) / windowDistance) * width;

            // Channel 1: Speed (Green: #00ff41, 0-400 km/h) -> Top 45% height
            drawChannelLine(visibleSlice, getX, (pt) => height * 0.45 * (1.0 - (pt.speed / 400.0)), '#00ff41', 2.0);

            // Channel 2: RPM (Purple: #a200ff, 0-15000 RPM) -> Top 45% height overlay
            drawChannelLine(visibleSlice, getX, (pt) => height * 0.45 * (1.0 - (pt.rpm / 15000.0)), '#a200ff', 1.5);

            // Channel 3: Throttle (Cyan: #00d2ff, 0-100%) -> Bottom 45% height (height*0.50 to height*0.92)
            drawChannelLine(visibleSlice, getX, (pt) => (height * 0.52) + (height * 0.40) * (1.0 - (pt.throttle / 100.0)), '#00d2ff', 1.5);

            // Channel 4: Brake (Red: #ff0000, 0-100%) -> Bottom 45% height
            drawChannelLine(visibleSlice, getX, (pt) => (height * 0.52) + (height * 0.40) * (1.0 - (pt.brake / 100.0)), '#ff0000', 1.8);

            // Channel 5: Steering (Magenta: #ff00ff, -50 to +50 deg)
            drawChannelLine(visibleSlice, getX, (pt) => (height * 0.52) + (height * 0.40) * (0.5 - (pt.steering / 100.0)), '#ff00ff', 1.5);
        }

        // 3. Draw Vertical Scrubber Indicator (Pinned at 80%)
        const scrubberX = SCRUBBER_X_RATIO * width;
        ctx.strokeStyle = '#ff5a1f';
        ctx.lineWidth = 2.0;
        ctx.shadowColor = '#ff5a1f';
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.moveTo(scrubberX, 0);
        ctx.lineTo(scrubberX, height);
        ctx.stroke();
        ctx.shadowBlur = 0; // reset glow

        // Scrubber Header Tag
        ctx.fillStyle = '#ff5a1f';
        ctx.fillRect(scrubberX - 35, 0, 70, 18);
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 9px monospace';
        ctx.textAlign = 'center';
        ctx.fillText('LIVE HUD', scrubberX, 12);
    }

    function drawChannelLine(slice, getX, getY, color, lineWidth) {
        if (slice.length < 2) return;
        ctx.strokeStyle = color;
        ctx.lineWidth = lineWidth;
        ctx.beginPath();

        for (let i = 0; i < slice.length; i++) {
            const x = getX(slice[i].distance);
            const y = getY(slice[i]);
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.stroke();
    }

    function drawGrid(width, height, startDist, endDist) {
        ctx.strokeStyle = '#14141e';
        ctx.lineWidth = 1;

        // Horizontal Gridlines
        const hBands = [0.15, 0.30, 0.45, 0.52, 0.72, 0.92];
        hBands.forEach(ratio => {
            const y = height * ratio;
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(width, y);
            ctx.stroke();
        });

        // Vertical Gridlines (every 100m or 200m)
        const tickStep = windowDistance <= 1000 ? 100 : 250;
        const firstTick = Math.ceil(startDist / tickStep) * tickStep;

        ctx.fillStyle = '#6b7280';
        ctx.font = '9px monospace';
        ctx.textAlign = 'center';

        for (let dist = firstTick; dist <= endDist; dist += tickStep) {
            const x = ((dist - startDist) / (endDist - startDist)) * width;
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, height);
            ctx.stroke();

            // Distance labels in kilometers
            const kmText = (dist / 1000.0).toFixed(2) + ' km';
            ctx.fillText(kmText, x, height - 6);
        }

        // Channel Category Separator Line
        ctx.strokeStyle = '#222234';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(0, height * 0.48);
        ctx.lineTo(width, height * 0.48);
        ctx.stroke();

        // Left Side Channel Range Legends
        ctx.textAlign = 'left';
        ctx.fillStyle = '#00ff41';
        ctx.fillText('SPEED: 400 KM/H', 10, 16);
        ctx.fillStyle = '#a200ff';
        ctx.fillText('RPM: 15,000', 120, 16);
        ctx.fillStyle = '#00d2ff';
        ctx.fillText('THROTTLE: 100%', 10, height * 0.52 + 12);
        ctx.fillStyle = '#ff0000';
        ctx.fillText('BRAKE: 100%', 120, height * 0.52 + 12);
        ctx.fillStyle = '#ff00ff';
        ctx.fillText('STEERING: ±50°', 210, height * 0.52 + 12);
    }

    // Binary search for visible slice culling
    function binarySearchLower(data, targetDist) {
        let low = 0, high = data.length - 1, ans = 0;
        while (low <= high) {
            const mid = (low + high) >> 1;
            if (data[mid].distance >= targetDist) {
                ans = mid;
                high = mid - 1;
            } else {
                low = mid + 1;
            }
        }
        return ans;
    }

    function binarySearchUpper(data, targetDist) {
        let low = 0, high = data.length - 1, ans = data.length - 1;
        while (low <= high) {
            const mid = (low + high) >> 1;
            if (data[mid].distance <= targetDist) {
                ans = mid;
                low = mid + 1;
            } else {
                high = mid - 1;
            }
        }
        return ans;
    }
})();
