// ============================================
// FUEL PUMP DASHBOARD - JavaScript
// ============================================

(function () {
    'use strict';

    // ============================================
    // STATE MANAGEMENT
    // ============================================

    let isDarkMode = true;
    let dispensersData = [];
    let updateInterval = null;

    // ============================================
    // INITIALIZATION
    // ============================================

    document.addEventListener('DOMContentLoaded', function () {
        initializeDashboard();
        setupEventListeners();
        startRealtimeUpdates();
        startDateTimeUpdates();
    });

    function initializeDashboard() {
        // Initialize dispensers data from DOM
        const dispenserCards = document.querySelectorAll('.dispenser-card');
        dispenserCards.forEach(card => {
            const dispenserId = card.getAttribute('data-dispenser-id');
            const nozzles = card.querySelectorAll('.nozzle-card');

            const dispenserData = {
                id: parseInt(dispenserId),
                element: card,
                status: card.querySelector('.status-badge').textContent.trim().toUpperCase(),
                isLocked: card.querySelector('.lock-btn').classList.contains('locked'),
                nozzles: []
            };

            nozzles.forEach((nozzle, index) => {
                const nozzleData = {
                    index: index + 1,
                    element: nozzle,
                    isFueling: false,
                    fuelType: nozzle.querySelector('.detail-value').textContent.trim(),
                    color: getNozzleColor(nozzle),
                    liters: parseFloat(nozzle.querySelector('[data-field="liters"]').textContent),
                    price: parseFloat(nozzle.querySelector('[data-field="price"]').textContent),
                    totalLiters: parseFloat(nozzle.querySelector('[data-field="totalLiters"]').textContent),
                    pricePerLiter: getPricePerLiter(nozzle)
                };
                dispenserData.nozzles.push(nozzleData);
            });

            dispensersData.push(dispenserData);
        });

        // Update stats
        updateStats();
    }

    function getNozzleColor(nozzleElement) {
        const svg = nozzleElement.querySelector('.fuel-nozzle');
        if (svg.classList.contains('green')) return 'green';
        if (svg.classList.contains('blue')) return 'blue';
        if (svg.classList.contains('gold')) return 'gold';
        return 'green';
    }

    function getPricePerLiter(nozzleElement) {
        const rows = nozzleElement.querySelectorAll('.detail-row');
        for (let row of rows) {
            if (row.textContent.includes('Unit Price')) {
                const text = row.querySelector('.detail-value').textContent;
                return parseFloat(text.replace('Rs.', '').trim());
            }
        }
        return 0;
    }

    // ============================================
    // EVENT LISTENERS
    // ============================================

    function setupEventListeners() {
        // Sidebar toggle
        const sidebarToggle = document.getElementById('sidebarToggle');
        const sidebar = document.getElementById('sidebar');

        if (sidebarToggle && sidebar) {
            sidebarToggle.addEventListener('click', function () {
                sidebar.classList.toggle('collapsed');
            });
        }

        // Theme toggle
        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', toggleTheme);
        }
    }

    // ============================================
    // THEME MANAGEMENT
    // ============================================

    function toggleTheme() {
        isDarkMode = !isDarkMode;
        document.body.classList.toggle('light-theme');

        const themeLabel = document.getElementById('themeLabel');
        const sunIcon = document.querySelector('.sun-icon');
        const moonIcon = document.querySelector('.moon-icon');
        const toggleText = document.querySelector('.toggle-text');

        if (isDarkMode) {
            themeLabel.textContent = 'Dark Mode';
            sunIcon.style.display = 'block';
            moonIcon.style.display = 'none';
            toggleText.textContent = 'Light';
        } else {
            themeLabel.textContent = 'Light Mode';
            sunIcon.style.display = 'none';
            moonIcon.style.display = 'block';
            toggleText.textContent = 'Dark';
        }
    }

    // ============================================
    // LOCK/UNLOCK FUNCTIONALITY
    // ============================================

    window.toggleLock = function (dispenserId) {
        const dispenser = dispensersData.find(d => d.id === dispenserId);
        if (!dispenser) return;

        dispenser.isLocked = !dispenser.isLocked;

        const lockBtn = dispenser.element.querySelector('.lock-btn');
        if (dispenser.isLocked) {
            lockBtn.classList.remove('unlocked');
            lockBtn.classList.add('locked');

            // Stop fueling when locked
            dispenser.nozzles.forEach(nozzle => {
                nozzle.isFueling = false;
                updateNozzleDisplay(nozzle);
            });
        } else {
            lockBtn.classList.remove('locked');
            lockBtn.classList.add('unlocked');
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
    // REAL-TIME UPDATES
    // ============================================

    function startRealtimeUpdates() {
        updateInterval = setInterval(function () {
            dispensersData.forEach(dispenser => {
                if (dispenser.status === 'OFFLINE' || dispenser.isLocked) {
                    // Stop fueling for offline or locked dispensers
                    dispenser.nozzles.forEach(nozzle => {
                        if (nozzle.isFueling) {
                            nozzle.isFueling = false;
                            updateNozzleDisplay(nozzle);
                        }
                    });
                    return;
                }

                dispenser.nozzles.forEach(nozzle => {
                    if (nozzle.isFueling) {
                        // Update values while fueling
                        const literIncrement = Math.random() * 2;
                        nozzle.liters += literIncrement;
                        nozzle.price = Math.round(nozzle.liters * nozzle.pricePerLiter);
                        nozzle.totalLiters += literIncrement;

                        // May stop fueling (30% chance)
                        if (Math.random() > 0.7) {
                            nozzle.isFueling = false;
                        }
                    } else {
                        // May start fueling (20% chance)
                        if (Math.random() > 0.8) {
                            nozzle.isFueling = true;
                            nozzle.liters = 0;
                            nozzle.price = 0;
                        }
                    }

                    updateNozzleDisplay(nozzle);
                });
            });

            updateStats();
        }, 2000);
    }

    function updateNozzleDisplay(nozzle) {
        // Update values
        const litersEl = nozzle.element.querySelector('[data-field="liters"]');
        const priceEl = nozzle.element.querySelector('[data-field="price"]');
        const totalLitersEl = nozzle.element.querySelector('[data-field="totalLiters"]');

        if (litersEl) litersEl.textContent = nozzle.liters.toFixed(3);
        if (priceEl) priceEl.textContent = nozzle.price.toFixed(2);
        if (totalLitersEl) totalLitersEl.textContent = nozzle.totalLiters.toFixed(3);

        // Update status
        const statusEl = nozzle.element.querySelector('.nozzle-status');
        if (statusEl) {
            if (nozzle.isFueling) {
                statusEl.textContent = 'FUELING';
                statusEl.className = 'nozzle-status fueling';
                startFuelingAnimation(nozzle);
            } else {
                statusEl.textContent = 'IN';
                statusEl.className = 'nozzle-status in';
                stopFuelingAnimation(nozzle);
                showStaticDrop(nozzle);
            }
        }
    }

    // ============================================
    // ANIMATIONS
    // ============================================

    function startFuelingAnimation(nozzle) {
        const dropletsContainer = nozzle.element.querySelector('.fuel-droplets');
        if (!dropletsContainer) return;

        // Clear existing animation
        dropletsContainer.innerHTML = '';

        // Add fueling class to nozzle SVG
        const nozzleSvg = nozzle.element.querySelector('.fuel-nozzle');
        if (nozzleSvg) {
            nozzleSvg.classList.add('fueling-animation');
        }

        // Create droplet interval
        nozzle.dropletInterval = setInterval(() => {
            createDroplet(dropletsContainer, nozzle.color);
        }, 200);
    }

    function stopFuelingAnimation(nozzle) {
        // Clear droplet interval
        if (nozzle.dropletInterval) {
            clearInterval(nozzle.dropletInterval);
            nozzle.dropletInterval = null;
        }

        // Remove fueling class
        const nozzleSvg = nozzle.element.querySelector('.fuel-nozzle');
        if (nozzleSvg) {
            nozzleSvg.classList.remove('fueling-animation');
        }

        // Clear droplets
        const dropletsContainer = nozzle.element.querySelector('.fuel-droplets');
        if (dropletsContainer) {
            setTimeout(() => {
                dropletsContainer.innerHTML = '';
            }, 700);
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

    function showStaticDrop(nozzle) {
        const dropletsContainer = nozzle.element.querySelector('.fuel-droplets');
        if (!dropletsContainer) return;

        dropletsContainer.innerHTML = `<div class="static-drop ${nozzle.color}"></div>`;
    }

    // ============================================
    // STATS CALCULATION
    // ============================================

    function updateStats() {
        let petrolTotal = 0;
        let dieselTotal = 0;
        let hioctaneTotal = 0;

        dispensersData.forEach(dispenser => {
            dispenser.nozzles.forEach(nozzle => {
                const fuelType = nozzle.fuelType.toUpperCase();
                if (fuelType.includes('PETROL')) {
                    petrolTotal += nozzle.totalLiters;
                } else if (fuelType.includes('DIESEL')) {
                    dieselTotal += nozzle.totalLiters;
                } else if (fuelType.includes('OCTANE')) {
                    hioctaneTotal += nozzle.totalLiters;
                }
            });
        });

        // Update stat cards
        const petrolEl = document.getElementById('petrolSales');
        const dieselEl = document.getElementById('dieselSales');
        const hioctaneEl = document.getElementById('hioctaneSales');

        if (petrolEl) petrolEl.textContent = petrolTotal.toFixed(1) + ' L';
        if (dieselEl) dieselEl.textContent = dieselTotal.toFixed(1) + ' L';
        if (hioctaneEl) hioctaneEl.textContent = hioctaneTotal.toFixed(1) + ' L';
    }

    // ============================================
    // UTILITY FUNCTIONS
    // ============================================

    function randomBetween(min, max) {
        return Math.random() * (max - min) + min;
    }

    // ============================================
    // API CALLS (For ASP.NET Integration)
    // ============================================

    // Function to fetch real data from ASP.NET API
    window.fetchDispenserData = function () {
        fetch('/api/dispensers')
            .then(response => response.json())
            .then(data => {
                // Update local state with server data
                console.log('Received dispenser data:', data);
                // Process and update UI
            })
            .catch(error => {
                console.error('Error fetching dispenser data:', error);
            });
    };

    // Function to send lock/unlock command to server
    window.sendLockCommand = function (dispenserId, isLocked) {
        fetch('/api/dispensers/' + dispenserId + '/lock', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ isLocked: isLocked })
        })
            .then(response => response.json())
            .then(data => {
                console.log('Lock command sent:', data);
            })
            .catch(error => {
                console.error('Error sending lock command:', error);
            });
    };

    // ============================================
    // CLEANUP
    // ============================================

    window.addEventListener('beforeunload', function () {
        if (updateInterval) {
            clearInterval(updateInterval);
        }

        dispensersData.forEach(dispenser => {
            dispenser.nozzles.forEach(nozzle => {
                if (nozzle.dropletInterval) {
                    clearInterval(nozzle.dropletInterval);
                }
            });
        });
    });

})();
