/**
 * OdontariDatePicker — Selector de fecha y hora para Odontari
 *
 * Flujo:
 *   1. Usuario hace clic en el trigger → se abre el overlay
 *   2. Tab "Fecha": calendario mensual para elegir día
 *   3. Tab "Hora": grid de slots (8:00-19:30), deshabilitados si ocupados o fuera del turno
 *   4. Al confirmar → actualiza el hidden input con formato "yyyy-MM-ddTHH:mm"
 *
 * No modifica ninguna lógica del servidor. Solo mejora la UX del picker.
 */
(function () {
    'use strict';

    var MESES = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];
    var DIAS_CORTO = ['Do','Lu','Ma','Mi','Ju','Vi','Sa'];
    var DIAS_LARGO = ['Domingo','Lunes','Martes','Miércoles','Jueves','Viernes','Sábado'];

    function pad(n) { return String(n).padStart(2, '0'); }

    function formatHora(h, m, fmt24) {
        if (fmt24) return pad(h) + ':' + pad(m);
        var ampm = h >= 12 ? 'PM' : 'AM';
        var h12 = h % 12 || 12;
        return h12 + ':' + pad(m) + '\u00a0' + ampm;
    }

    function formatFechaLarga(y, mo, d) {
        var dt = new Date(y, mo, d);
        return DIAS_LARGO[dt.getDay()] + ' ' + d + ' de ' + MESES[mo] + ' ' + y;
    }

    // Genera slots 08:00 → 19:30, cada 30 min (igual que el grid de la agenda)
    function generarSlots() {
        var slots = [];
        for (var h = 8; h <= 19; h++) {
            slots.push({ h: h, m: 0 });
            if (h < 19) slots.push({ h: h, m: 30 });
        }
        return slots;
    }

    var _uidCounter = 0;

    function OdontariDatePicker(opts) {
        this._id = 'odp' + (++_uidCounter);
        this.trigger      = opts.trigger;
        this.hiddenInput  = opts.hiddenInput;
        this.doctorSelect = opts.doctorSelect || null;
        this.horasUrl     = opts.horasOcupadasUrl || '/Clinica/Agenda/GetHorasOcupadas';
        this.onSelect     = opts.onSelect || function () {};

        this.fmt24 = true;
        this._tab  = 'cal';

        // Estado seleccionado
        var iv = this.hiddenInput ? this.hiddenInput.value : '';
        var parsed = iv ? this._parseISO(iv) : null;
        var base = parsed || new Date();
        this._y  = base.getFullYear();
        this._mo = base.getMonth();
        this._d  = base.getDate();
        this._h  = parsed ? base.getHours() : 9;
        this._m  = parsed ? (base.getMinutes() >= 30 ? 30 : 0) : 0;

        // Estado del navegador del calendario
        this._calY  = this._y;
        this._calMo = this._mo;

        // Slots ocupados y horario del doctor (cargados via AJAX)
        this._ocupadas    = [];
        this._horaEntrada = null; // "HH:mm"
        this._horaSalida  = null; // "HH:mm"
        this._fetching    = false;
        this._lastFetchKey = '';

        this._buildDOM();
        this._bindTrigger();
        this._refreshTriggerText();
    }

    OdontariDatePicker.prototype._parseISO = function (s) {
        // "yyyy-MM-ddTHH:mm" — evitar el bug de UTC de `new Date(s)`
        var m = s.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
        if (!m) return null;
        return new Date(+m[1], +m[2]-1, +m[3], +m[4], +m[5]);
    };

    OdontariDatePicker.prototype._uid = function (s) {
        return this._id + '_' + s;
    };

    OdontariDatePicker.prototype._buildDOM = function () {
        var id = this._id;
        var el = document.createElement('div');
        el.className = 'odp-overlay';
        el.id = id + '_overlay';
        el.setAttribute('role', 'dialog');
        el.setAttribute('aria-modal', 'true');
        el.setAttribute('aria-label', 'Seleccionar fecha y hora');
        el.innerHTML =
            '<div class="odp-panel">' +
                '<div class="odp-header">' +
                    '<div class="odp-header-label">Fecha y hora de la cita</div>' +
                    '<div class="odp-header-date" id="' + id + '_hdate">\u2014</div>' +
                    '<div class="odp-header-time" id="' + id + '_htime">\u2014</div>' +
                '</div>' +
                '<div class="odp-tabs">' +
                    '<button type="button" class="odp-tab active" data-odptab="cal">\uD83D\uDCC5\u00a0Fecha</button>' +
                    '<button type="button" class="odp-tab" data-odptab="time">\uD83D\uDD50\u00a0Hora</button>' +
                '</div>' +
                '<div class="odp-tab-content" id="' + id + '_content"></div>' +
                '<div class="odp-footer">' +
                    '<button type="button" class="odp-btn-cancel" id="' + id + '_cancel">Cancelar</button>' +
                    '<button type="button" class="odp-btn-confirm" id="' + id + '_confirm">Confirmar</button>' +
                '</div>' +
            '</div>';
        document.body.appendChild(el);
        this._overlay = el;
        this._hdate   = document.getElementById(id + '_hdate');
        this._htime   = document.getElementById(id + '_htime');
        this._content = document.getElementById(id + '_content');

        var self = this;

        // Cerrar al hacer clic en el backdrop
        el.addEventListener('click', function (e) {
            if (e.target === el) self.close();
        });

        // Tabs
        el.querySelectorAll('[data-odptab]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                self._switchTab(btn.getAttribute('data-odptab'));
            });
        });

        // Botones footer
        document.getElementById(id + '_cancel').addEventListener('click', function () { self.close(); });
        document.getElementById(id + '_confirm').addEventListener('click', function () { self._confirm(); });
    };

    OdontariDatePicker.prototype._bindTrigger = function () {
        var self = this;
        if (this.trigger) {
            this.trigger.addEventListener('click', function (e) {
                e.preventDefault();
                self.open();
            });
        }
        // Cuando cambia el doctor → re-fetch slots si el picker está abierto o al próximo open
        if (this.doctorSelect) {
            this.doctorSelect.addEventListener('change', function () {
                self._ocupadas    = [];
                self._horaEntrada = null;
                self._horaSalida  = null;
                self._lastFetchKey = '';
                if (self._overlay.classList.contains('odp-open') && self._tab === 'time') {
                    self._fetchSlots();
                }
            });
        }
    };

    OdontariDatePicker.prototype.open = function () {
        this._overlay.classList.add('odp-open');
        document.body.style.overflow = 'hidden';
        this._switchTab('cal', true);
        this._updateHeader();
    };

    OdontariDatePicker.prototype.close = function () {
        this._overlay.classList.remove('odp-open');
        document.body.style.overflow = '';
    };

    OdontariDatePicker.prototype._confirm = function () {
        var iso = this._y + '-' + pad(this._mo + 1) + '-' + pad(this._d) + 'T' + pad(this._h) + ':' + pad(this._m);
        if (this.hiddenInput) this.hiddenInput.value = iso;
        this._refreshTriggerText();
        this.onSelect(iso);
        this.close();
    };

    OdontariDatePicker.prototype._switchTab = function (tab, skipRender) {
        this._tab = tab;
        this._overlay.querySelectorAll('[data-odptab]').forEach(function (btn) {
            btn.classList.toggle('active', btn.getAttribute('data-odptab') === tab);
        });
        if (!skipRender) {
            if (tab === 'time') {
                this._fetchSlots();
            } else {
                this._renderCalendar();
            }
        } else {
            this._renderCalendar();
        }
    };

    OdontariDatePicker.prototype._updateHeader = function () {
        this._hdate.textContent = formatFechaLarga(this._y, this._mo, this._d);
        this._htime.textContent = formatHora(this._h, this._m, this.fmt24);
    };

    OdontariDatePicker.prototype._refreshTriggerText = function () {
        if (!this.trigger) return;
        var textEl = this.trigger.querySelector('.odp-trigger-text');
        var fechaStr = formatFechaLarga(this._y, this._mo, this._d);
        var horaStr  = formatHora(this._h, this._m, this.fmt24);
        var display  = fechaStr + '\u2002\u2014\u2002' + horaStr;
        if (textEl) {
            textEl.textContent = display;
            textEl.classList.remove('placeholder');
        }
    };

    // ──────────────────────────────────────────────────────
    // CALENDARIO
    // ──────────────────────────────────────────────────────
    OdontariDatePicker.prototype._renderCalendar = function () {
        var y  = this._calY;
        var mo = this._calMo;
        var firstDow   = new Date(y, mo, 1).getDay();
        var daysInMon  = new Date(y, mo + 1, 0).getDate();
        var daysInPrev = new Date(y, mo, 0).getDate();

        var today = new Date();
        var todayY = today.getFullYear(), todayMo = today.getMonth(), todayD = today.getDate();
        // Fecha mínima: hoy (DR = UTC-4, pero solo la fecha importa para el picker)
        var minDate = new Date(todayY, todayMo, todayD);

        var id = this._id;
        var self = this;

        var html = '<div class="odp-calendar">' +
            '<div class="odp-cal-nav">' +
                '<button type="button" class="odp-cal-btn" id="' + id + '_prev">&#8249;</button>' +
                '<span class="odp-cal-month">' + MESES[mo] + ' ' + y + '</span>' +
                '<button type="button" class="odp-cal-btn" id="' + id + '_next">&#8250;</button>' +
            '</div>' +
            '<div class="odp-cal-grid">';

        DIAS_CORTO.forEach(function (d) {
            html += '<div class="odp-cal-dow">' + d + '</div>';
        });

        // Días del mes anterior (decorativos, deshabilitados)
        for (var i = 0; i < firstDow; i++) {
            html += '<button type="button" class="odp-cal-day odp-cal-day-other" disabled>' +
                (daysInPrev - firstDow + 1 + i) + '</button>';
        }

        // Días del mes actual
        for (var d = 1; d <= daysInMon; d++) {
            var dt = new Date(y, mo, d);
            var isPast   = dt < minDate;
            var isToday  = y === todayY && mo === todayMo && d === todayD;
            var isSel    = y === this._y && mo === this._mo && d === this._d;
            var cls = 'odp-cal-day';
            if (isToday)  cls += ' odp-cal-day-today';
            if (isSel)    cls += ' odp-cal-day-selected';
            html += '<button type="button" class="' + cls + '" data-day="' + d + '"' +
                (isPast ? ' disabled' : '') + '>' + d + '</button>';
        }

        // Relleno posterior
        var total = firstDow + daysInMon;
        var fill  = total % 7 === 0 ? 0 : 7 - (total % 7);
        for (var j = 1; j <= fill; j++) {
            html += '<button type="button" class="odp-cal-day odp-cal-day-other" disabled>' + j + '</button>';
        }

        html += '</div></div>';
        this._content.innerHTML = html;

        // Eventos de navegación
        document.getElementById(id + '_prev').addEventListener('click', function () {
            self._calMo--;
            if (self._calMo < 0) { self._calMo = 11; self._calY--; }
            self._renderCalendar();
        });
        document.getElementById(id + '_next').addEventListener('click', function () {
            self._calMo++;
            if (self._calMo > 11) { self._calMo = 0; self._calY++; }
            self._renderCalendar();
        });

        // Selección de día → ir a tab de hora
        this._content.querySelectorAll('.odp-cal-day[data-day]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                self._y  = self._calY;
                self._mo = self._calMo;
                self._d  = parseInt(btn.getAttribute('data-day'), 10);
                self._updateHeader();
                // Invalidar cache de slots si cambia la fecha
                self._ocupadas    = [];
                self._horaEntrada = null;
                self._horaSalida  = null;
                self._lastFetchKey = '';
                self._switchTab('time');
            });
        });
    };

    // ──────────────────────────────────────────────────────
    // FETCH SLOTS OCUPADOS
    // ──────────────────────────────────────────────────────
    OdontariDatePicker.prototype._fetchSlots = function () {
        var doctorId = this.doctorSelect ? this.doctorSelect.value : '';
        var fecha    = this._y + '-' + pad(this._mo + 1) + '-' + pad(this._d);
        var key = doctorId + '|' + fecha;

        if (key === this._lastFetchKey) {
            // Ya tenemos los datos para este doctor+fecha
            this._renderTimeGrid();
            return;
        }

        if (!doctorId) {
            this._ocupadas    = [];
            this._horaEntrada = null;
            this._horaSalida  = null;
            this._lastFetchKey = key;
            this._renderTimeGrid();
            return;
        }

        var self = this;
        this._fetching = true;
        this._renderTimeGrid(); // muestra loading

        fetch(this.horasUrl + '?doctorId=' + encodeURIComponent(doctorId) + '&fecha=' + encodeURIComponent(fecha))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                self._ocupadas    = data.ocupadas || [];
                self._horaEntrada = data.horaEntrada || null;
                self._horaSalida  = data.horaSalida  || null;
                self._lastFetchKey = key;
                self._fetching = false;
                self._renderTimeGrid();
            })
            .catch(function () {
                self._ocupadas    = [];
                self._fetching    = false;
                self._lastFetchKey = key;
                self._renderTimeGrid();
            });
    };

    OdontariDatePicker.prototype._slotMinutos = function (h, m) {
        return h * 60 + m;
    };

    OdontariDatePicker.prototype._isPasado = function (h, m) {
        var now = new Date();
        var hoy = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        var sel = new Date(this._y, this._mo, this._d);
        if (sel > hoy) return false; // fecha futura → ninguna hora es pasada
        if (sel < hoy) return true;  // fecha pasada → todos los slots son pasados
        // Es hoy: comparar minutos
        var slotMins = h * 60 + m;
        var ahoraMins = now.getHours() * 60 + now.getMinutes();
        return slotMins < ahoraMins; // sin margen extra: el servidor ya tiene 30 min de tolerancia
    };

    OdontariDatePicker.prototype._isOcupado = function (h, m) {
        var inicio = this._slotMinutos(h, m);
        var fin    = inicio + 30;
        return this._ocupadas.some(function (o) {
            var parts = o.inicio.split(':'), fparts = o.fin.split(':');
            var oInicio = parseInt(parts[0], 10) * 60 + parseInt(parts[1], 10);
            var oFin    = parseInt(fparts[0], 10) * 60 + parseInt(fparts[1], 10);
            return inicio < oFin && oInicio < fin;
        });
    };

    OdontariDatePicker.prototype._isFueraDeHorario = function (h, m) {
        if (!this._horaEntrada || !this._horaSalida) return false;
        var ep = this._horaEntrada.split(':'), sp = this._horaSalida.split(':');
        var entMins = parseInt(ep[0], 10) * 60 + parseInt(ep[1], 10);
        var salMins = parseInt(sp[0], 10) * 60 + parseInt(sp[1], 10);
        var slotMins = this._slotMinutos(h, m);
        return slotMins < entMins || slotMins >= salMins;
    };

    // ──────────────────────────────────────────────────────
    // GRID DE HORAS
    // ──────────────────────────────────────────────────────
    OdontariDatePicker.prototype._renderTimeGrid = function () {
        var self    = this;
        var slots   = generarSlots();
        var sinDoctor = this.doctorSelect && !this.doctorSelect.value;

        var html = '<div class="odp-time-section">' +
            '<div class="odp-time-toolbar">' +
                '<span class="odp-time-label">Selecciona la hora</span>' +
                '<div class="odp-fmt-toggle">' +
                    '<button type="button" class="odp-fmt-btn' + (!this.fmt24 ? ' active' : '') + '" data-odpfmt="12">12h</button>' +
                    '<button type="button" class="odp-fmt-btn' + (this.fmt24  ? ' active' : '') + '" data-odpfmt="24">24h</button>' +
                '</div>' +
            '</div>' +
            '<div class="odp-slots-grid">';

        if (this._fetching) {
            html += '<div class="odp-slots-loading">Cargando disponibilidad\u2026</div>';
        } else if (sinDoctor) {
            html += '<div class="odp-no-doctor-msg">Selecciona un doctor para ver<br>la disponibilidad de horarios.</div>';
            slots.forEach(function (s) {
                var isSel = s.h === self._h && s.m === self._m;
                html += '<button type="button" class="odp-slot' + (isSel ? ' odp-slot-selected' : '') +
                    '" data-h="' + s.h + '" data-m="' + s.m + '">' + formatHora(s.h, s.m, self.fmt24) + '</button>';
            });
        } else {
            slots.forEach(function (s) {
                var isSel   = s.h === self._h && s.m === self._m;
                var pasado  = self._isPasado(s.h, s.m);
                var ocupado = !pasado && self._isOcupado(s.h, s.m);
                var fuera   = !pasado && self._isFueraDeHorario(s.h, s.m);
                var cls = 'odp-slot';
                if (isSel)        cls += ' odp-slot-selected';
                else if (pasado)  cls += ' odp-slot-past';
                else if (ocupado) cls += ' odp-slot-busy';
                else if (fuera)   cls += ' odp-slot-outside';
                var disabled = pasado || ocupado || fuera;
                html += '<button type="button" class="' + cls + '" data-h="' + s.h + '" data-m="' + s.m + '"' +
                    (disabled ? ' disabled' : '') + '>' + formatHora(s.h, s.m, self.fmt24) + '</button>';
            });
        }

        html += '</div></div>';
        this._content.innerHTML = html;

        // Toggle 12h/24h
        this._content.querySelectorAll('[data-odpfmt]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                self.fmt24 = btn.getAttribute('data-odpfmt') === '24';
                self._updateHeader();
                self._refreshTriggerText();
                self._renderTimeGrid();
            });
        });

        // Selección de slot
        this._content.querySelectorAll('.odp-slot[data-h]').forEach(function (btn) {
            if (!btn.disabled) {
                btn.addEventListener('click', function () {
                    self._h = parseInt(btn.getAttribute('data-h'), 10);
                    self._m = parseInt(btn.getAttribute('data-m'), 10);
                    self._updateHeader();
                    self._renderTimeGrid();
                });
            }
        });
    };

    // Exponer globalmente
    window.OdontariDatePicker = OdontariDatePicker;
})();
