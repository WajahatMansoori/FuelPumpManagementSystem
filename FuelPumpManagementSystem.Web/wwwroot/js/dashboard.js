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
        
        // Handle Enter key press in access key input
        const accessKeyInput = document.getElementById('lockAccessKey');
        if (accessKeyInput) {
            accessKeyInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    submitLockAccessKey();
                }
            });
        }
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

    let pendingLockDispenserId = null;

    window.toggleLock = function (dispenserId) {
        // Store the dispenser ID for later use
        pendingLockDispenserId = dispenserId;
        
        // Show the access key modal
        const modal = document.getElementById('lockAccessKeyModal');
        const accessKeyInput = document.getElementById('lockAccessKey');
        const errorDiv = document.getElementById('lockAccessKeyError');
        
        if (modal) {
            modal.style.display = 'flex';
            if (accessKeyInput) {
                accessKeyInput.value = '';
                accessKeyInput.focus();
            }
            if (errorDiv) {
                errorDiv.style.display = 'none';
            }
        }
    };

    window.closeLockAccessKeyModal = function () {
        const modal = document.getElementById('lockAccessKeyModal');
        const accessKeyInput = document.getElementById('lockAccessKey');
        const errorDiv = document.getElementById('lockAccessKeyError');
        
        if (modal) {
            modal.style.display = 'none';
        }
        if (accessKeyInput) {
            accessKeyInput.value = '';
        }
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        
        pendingLockDispenserId = null;
    };

    window.submitLockAccessKey = async function () {
        const accessKeyInput = document.getElementById('lockAccessKey');
        const errorDiv = document.getElementById('lockAccessKeyError');
        const submitBtn = document.querySelector('.lock-btn-submit');
        
        if (!accessKeyInput || !errorDiv) return;
        
        const accessKey = accessKeyInput.value.trim();
        
        if (!accessKey) {
            errorDiv.textContent = 'Please enter an access key';
            errorDiv.style.display = 'flex';
            return;
        }
        
        // Disable submit button during validation
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Verifying...';
        }
        
        try {
            // Validate access key
            const validateResponse = await fetch('/Dashboard/ValidateLockAccessKey', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    AccessKey: accessKey
                })
            });
            
            const validateResult = await validateResponse.json();
            
            if (validateResult.success) {
                // Access key is valid, proceed with lock/unlock
                await performLockUnlock(pendingLockDispenserId);
                closeLockAccessKeyModal();
            } else {
                // Show error message
                errorDiv.textContent = validateResult.message || 'Invalid Access Key';
                errorDiv.style.display = 'flex';
            }
        } catch (error) {
            errorDiv.textContent = 'An error occurred. Please try again.';
            errorDiv.style.display = 'flex';
        } finally {
            // Re-enable submit button
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Verify & Proceed';
            }
        }
    };

    async function performLockUnlock(dispenserId) {
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
    }

    // ============================================
    // CHANGE PASSWORD FUNCTIONALITY
    // ============================================

    window.openChangePasswordModal = function () {
        const modal = document.getElementById('changePasswordModal');
        const newPasswordInput = document.getElementById('newPassword');
        const confirmPasswordInput = document.getElementById('confirmPassword');
        const errorDiv = document.getElementById('changePasswordError');
        const successDiv = document.getElementById('changePasswordSuccess');
        
        if (modal) {
            modal.style.display = 'flex';
            if (newPasswordInput) {
                newPasswordInput.value = '';
                newPasswordInput.focus();
            }
            if (confirmPasswordInput) {
                confirmPasswordInput.value = '';
            }
            if (errorDiv) {
                errorDiv.style.display = 'none';
            }
            if (successDiv) {
                successDiv.style.display = 'none';
            }
        }
    };

    window.closeChangePasswordModal = function () {
        const modal = document.getElementById('changePasswordModal');
        const newPasswordInput = document.getElementById('newPassword');
        const confirmPasswordInput = document.getElementById('confirmPassword');
        const errorDiv = document.getElementById('changePasswordError');
        const successDiv = document.getElementById('changePasswordSuccess');
        
        if (modal) {
            modal.style.display = 'none';
        }
        if (newPasswordInput) {
            newPasswordInput.value = '';
        }
        if (confirmPasswordInput) {
            confirmPasswordInput.value = '';
        }
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (successDiv) {
            successDiv.style.display = 'none';
        }
    };

    window.submitChangePassword = async function () {
        const newPasswordInput = document.getElementById('newPassword');
        const confirmPasswordInput = document.getElementById('confirmPassword');
        const errorDiv = document.getElementById('changePasswordError');
        const successDiv = document.getElementById('changePasswordSuccess');
        const submitBtn = document.querySelector('#changePasswordModal .lock-btn-submit');
        
        if (!newPasswordInput || !confirmPasswordInput || !errorDiv || !successDiv) return;
        
        const newPassword = newPasswordInput.value.trim();
        const confirmPassword = confirmPasswordInput.value.trim();
        
        // Hide previous messages
        errorDiv.style.display = 'none';
        successDiv.style.display = 'none';
        
        // Validation
        if (!newPassword) {
            errorDiv.textContent = 'Please enter a new password';
            errorDiv.style.display = 'flex';
            return;
        }
        
        if (newPassword.length < 4) {
            errorDiv.textContent = 'Password must be at least 4 characters long';
            errorDiv.style.display = 'flex';
            return;
        }
        
        if (!confirmPassword) {
            errorDiv.textContent = 'Please confirm your password';
            errorDiv.style.display = 'flex';
            return;
        }
        
        if (newPassword !== confirmPassword) {
            errorDiv.textContent = 'Passwords do not match. Please try again.';
            errorDiv.style.display = 'flex';
            return;
        }
        
        // Disable submit button during processing
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Saving...';
        }
        
        try {
            const response = await fetch('/Dashboard/ChangePassword', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    NewPassword: newPassword
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                successDiv.textContent = result.message || 'Password changed successfully!';
                successDiv.style.display = 'flex';
                
                // Clear inputs
                newPasswordInput.value = '';
                confirmPasswordInput.value = '';
                
                // Close modal after 2 seconds
                setTimeout(() => {
                    closeChangePasswordModal();
                }, 2000);
            } else {
                errorDiv.textContent = result.message || 'Failed to change password';
                errorDiv.style.display = 'flex';
            }
        } catch (error) {
            errorDiv.textContent = 'An error occurred. Please try again.';
            errorDiv.style.display = 'flex';
        } finally {
            // Re-enable submit button
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Save Password';
            }
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
