// ============================================
// FUEL PUMP DASHBOARD - JavaScript (SignalR ONLY)
// NO LOCAL DATA MANIPULATION - DISPLAY ONLY
// ============================================

(function () {
    'use strict';

    // ============================================
    // STATE MANAGEMENT
    // ============================================

    let connection = null;
    let activeAnimations = new Map();

    // ============================================
    // INITIALIZATION
    // ============================================

    document.addEventListener('DOMContentLoaded', function () {
        startDateTimeUpdates();
        initializeSignalR();
    });


    // ============================================
    // SIGNALR CONNECTION
    // ============================================

    function initializeSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/dashboardHub")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveDispenserUpdate", function (data) {
            updateDispenserUI(data);
        });

        connection.start()
            .then(function () {
                // SignalR Connected
            })
            .catch(function (err) {
                console.error("SignalR Connection Error:", err);
                setTimeout(initializeSignalR, 5000);
            });

        connection.onreconnected(function () {
            // SignalR Reconnected
        });
    }

    function updateDispenserUI(data) {
        const dispenserCard = document.querySelector(`[data-dispenser-id="${data.dispenserId}"]`);
        if (!dispenserCard) return;

        // Update dispenser status
        const statusBadge = dispenserCard.querySelector('.status-badge');
        const statusDot = dispenserCard.querySelector('.status-dot');
        if (statusBadge && statusDot) {
            const status = data.isOnline ? 'ONLINE' : 'OFFLINE';
            statusBadge.textContent = status;
            statusBadge.className = `status-badge ${status.toLowerCase()}`;
            statusDot.className = `status-dot ${status.toLowerCase()}`;
        }

        // Update lock button
        const lockBtn = dispenserCard.querySelector('.lock-btn');
        if (lockBtn) {
            if (data.isLocked) {
                lockBtn.classList.remove('unlocked');
                lockBtn.classList.add('locked');
            } else {
                lockBtn.classList.remove('locked');
                lockBtn.classList.add('unlocked');
            }
        }

        // Update nozzle 1
        if (data.nozzle1) {
            updateNozzleUI(dispenserCard, 1, data.nozzle1);
        }

        // Update nozzle 2
        if (data.nozzle2) {
            updateNozzleUI(dispenserCard, 2, data.nozzle2);
        }
    }

    function updateNozzleUI(dispenserCard, nozzleNumber, nozzleData) {
        const nozzleCards = dispenserCard.querySelectorAll('.nozzle-card');
        const nozzleCard = nozzleCards[nozzleNumber - 1];
        if (!nozzleCard) return;

        // Check if nozzle is disabled
        const isDisabled = nozzleData.isEnabled === false || nozzleData.status === 'DISABLED';

        if (isDisabled) {
            // Apply disabled styling
            nozzleCard.classList.add('disabled');
            
            // Update status to DISABLED
            const statusEl = nozzleCard.querySelector('.nozzle-status');
            if (statusEl) {
                statusEl.textContent = 'DISABLED';
                statusEl.className = 'nozzle-status disabled';
            }

            // Set all values to N/A
            const fuelTypeEl = nozzleCard.querySelector('.detail-value');
            if (fuelTypeEl) fuelTypeEl.textContent = 'N/A';

            const priceEl = nozzleCard.querySelector('[data-field="price"]');
            if (priceEl) priceEl.parentElement.innerHTML = 'Rs. <span data-field="price">N/A</span>';

            const litersEl = nozzleCard.querySelector('[data-field="liters"]');
            if (litersEl) litersEl.parentElement.innerHTML = '<span data-field="liters">N/A</span> L';

            const totalLitersEl = nozzleCard.querySelector('[data-field="totalLiters"]');
            if (totalLitersEl) totalLitersEl.parentElement.innerHTML = '<span data-field="totalLiters">N/A</span> L';

            // Stop any animations
            const nozzleKey = `${dispenserCard.getAttribute('data-dispenser-id')}-nozzle-${nozzleNumber}`;
            stopFuelingAnimation(nozzleKey);

            return;
        }

        // Remove disabled class if previously disabled
        nozzleCard.classList.remove('disabled');

        // CRITICAL: ONLY UPDATE VALUES FROM SERVER - NO LOCAL MANIPULATION
        const litersEl = nozzleCard.querySelector('[data-field="liters"]');
        const priceEl = nozzleCard.querySelector('[data-field="price"]');
        const totalLitersEl = nozzleCard.querySelector('[data-field="totalLiters"]');

        // Display exact values from server without any modification
        if (litersEl) litersEl.textContent = nozzleData.liters.toFixed(3);
        if (priceEl) priceEl.textContent = nozzleData.price.toFixed(2);
        if (totalLitersEl) totalLitersEl.textContent = nozzleData.totalLiters.toFixed(3);

        // Update nozzle status
        const statusEl = nozzleCard.querySelector('.nozzle-status');
        if (statusEl) {
            const currentStatus = statusEl.textContent.trim().toUpperCase();
            const newStatus = nozzleData.status.toUpperCase();
            
            statusEl.textContent = newStatus;
            statusEl.className = `nozzle-status ${newStatus.toLowerCase()}`;

            // Handle animations ONLY on status change
            const dropletsContainer = nozzleCard.querySelector('.fuel-droplets');
            if (!dropletsContainer) return;
            
            const color = dropletsContainer.getAttribute('data-color') || 'green';
            const nozzleKey = `${dispenserCard.getAttribute('data-dispenser-id')}-nozzle-${nozzleNumber}`;

            if (newStatus === 'FUELING' && currentStatus !== 'FUELING') {
                startFuelingAnimation(dropletsContainer, color, nozzleKey);
            } else if (newStatus !== 'FUELING' && currentStatus === 'FUELING') {
                stopFuelingAnimation(nozzleKey);
            }
        }
    }

    // ============================================
    // LOCK/UNLOCK FUNCTIONALITY
    // ============================================

    window.toggleLock = async function (dispenserId) {
        const dispenserCard = document.querySelector(`[data-dispenser-id="${dispenserId}"]`);
        if (!dispenserCard) return;

        const lockBtn = dispenserCard.querySelector('.lock-btn');
        const isCurrentlyLocked = lockBtn.classList.contains('locked');
        const newLockState = !isCurrentlyLocked;

        try {
            const response = await fetch(`/Dispenser/LockUnlockDispenser`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    DispenserId: dispenserId,
                    IsLocked: newLockState
                })
            });

            const result = await response.json();

            if (result.success) {
                // Update UI based on new lock state
                if (newLockState) {
                    lockBtn.classList.remove('unlocked');
                    lockBtn.classList.add('locked');
                } else {
                    lockBtn.classList.remove('locked');
                    lockBtn.classList.add('unlocked');
                }
            }
        } catch (error) {
            // Silently fail - no popup
        }
    };

    // ============================================
    // DATE/TIME UPDATES
    // ============================================

    function startDateTimeUpdates() {
        updateDateTime();
        setInterval(updateDateTime, 1000);
    }

    function updateDateTime() {
        const dateTimeElement = document.getElementById('currentDateTime');
        if (!dateTimeElement) return;

        const now = new Date();
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        
        const dayName = days[now.getDay()];
        const day = now.getDate();
        const month = months[now.getMonth()];
        const year = now.getFullYear();
        
        let hours = now.getHours();
        const minutes = now.getMinutes();
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        hours = hours ? hours : 12;
        const minutesStr = minutes < 10 ? '0' + minutes : minutes;
        
        const formattedDateTime = `${dayName}, ${day} ${month} ${year}, ${hours}:${minutesStr}${ampm}`;
        dateTimeElement.textContent = formattedDateTime;
    }


    // ============================================
    // ANIMATIONS (Status-Based: ONLY FUELING)
    // ============================================

    function startFuelingAnimation(dropletsContainer, color, nozzleKey) {
        if (!dropletsContainer) return;

        // Stop existing animation if any
        stopFuelingAnimation(nozzleKey);

        // Clear existing droplets
        dropletsContainer.innerHTML = '';

        // Create droplet interval - ONLY for FUELING status
        const intervalId = setInterval(() => {
            createDroplet(dropletsContainer, color);
        }, 200);

        // Store interval ID for cleanup
        activeAnimations.set(nozzleKey, intervalId);
    }

    function stopFuelingAnimation(nozzleKey) {
        // Clear droplet interval
        const intervalId = activeAnimations.get(nozzleKey);
        if (intervalId) {
            clearInterval(intervalId);
            activeAnimations.delete(nozzleKey);
        }
    }

    function createDroplet(container, color) {
        const droplet = document.createElement('div');
        droplet.className = `droplet ${color}`;
        droplet.style.left = `${(Math.random() - 0.5) * 6}px`;

        container.appendChild(droplet);

        // Remove droplet after animation
        setTimeout(() => {
            if (droplet.parentNode) {
                droplet.parentNode.removeChild(droplet);
            }
        }, 700);
    }


    // ============================================
    // CLEANUP
    // ============================================

    window.addEventListener('beforeunload', function () {
        // Clear all active animations
        activeAnimations.forEach((intervalId) => {
            clearInterval(intervalId);
        });
        activeAnimations.clear();

        // Stop SignalR connection
        if (connection) {
            connection.stop();
        }
    });

})();
