/**
 * Periodontograma - Integración Odontari.
 * Misma lógica que el proyecto de referencia (Desktop/periodontograma/app.js).
 * Carga/guardado vía API en lugar de localStorage.
 */
(function () {
  'use strict';

  var TEETH_SUPERIOR = [18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28];
  var TEETH_INFERIOR = [48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38];

  function createEmptyToothData() {
    return {
      ausencia: false,
      implante: false,
      movilidad: '0',
      pronostico: 'Bueno',
      furca: '0',
      sangrado: { M: false, C: false, D: false },
      supuracion: { M: false, C: false, D: false },
      placa: { M: false, C: false, D: false },
      anchuraEncia: '',
      margenVestibular: { M: '', C: '', D: '' },
      sondajeVestibular: { M: '', C: '', D: '' },
      margenPalatal: { M: '', C: '', D: '' },
      sondajePalatal: { M: '', C: '', D: '' }
    };
  }

  var state = { superior: {}, inferior: {} };
  var SITES = ['M', 'C', 'D'];

  var ROWS = [
    { id: 'ausencia', label: 'AUSENCIA', type: 'checkbox', key: 'ausencia', sectionLabel: 'Cara vestibular', group: 1 },
    { id: 'implante', label: 'IMPLANTE', type: 'checkbox', key: 'implante', group: 1, groupEnd: true },
    { id: 'movilidad', label: 'MOVILIDAD', type: 'select', key: 'movilidad', options: ['0', '1', '2', '3'], group: 2 },
    { id: 'pronostico', label: 'PRONÓSTICO', type: 'select', key: 'pronostico', options: ['Bueno', 'Reservado', 'Malo'], group: 2 },
    { id: 'furca', label: 'FURCA', type: 'select', key: 'furca', options: ['0', 'I', 'II', 'III'], group: 2, groupEnd: true },
    { id: 'sangrado', label: 'SANGRADO', type: 'miniMCD', key: 'sangrado', group: 3 },
    { id: 'supuracion', label: 'SUPURACIÓN', type: 'miniMCD', key: 'supuracion', group: 3 },
    { id: 'placa', label: 'PLACA', type: 'miniMCD', key: 'placa', group: 3, groupEnd: true },
    { id: 'anchuraEncia', label: 'ANCHURA ENCÍA', type: 'number', key: 'anchuraEncia', min: 0, max: 9, group: 4 },
    { id: 'margenVestibular', label: 'MARGEN GINGIVAL (V)', type: 'tripleNumber', key: 'margenVestibular', min: -9, max: 9, group: 4 },
    { id: 'sondajeVestibular', label: 'PROF. SONDAJE (V)', type: 'tripleNumber', key: 'sondajeVestibular', min: 0, max: 12, group: 4 },
    { id: 'teethDraw', label: '', type: 'teethDraw', key: null, group: 4 },
    { id: 'margenPalatal', label: 'MARGEN GINGIVAL (P/L)', type: 'tripleNumber', key: 'margenPalatal', min: -9, max: 9, sectionLabel: 'Cara palatina / lingual', group: 4 },
    { id: 'sondajePalatal', label: 'PROF. SONDAJE (P/L)', type: 'tripleNumber', key: 'sondajePalatal', min: 0, max: 12, group: 4, groupEnd: true }
  ];

  function byId(id) { return document.getElementById(id); }
  function createEl(tag, className, attrs) {
    var el = document.createElement(tag);
    if (className) el.className = className;
    if (attrs) Object.keys(attrs).forEach(function (k) { el.setAttribute(k, attrs[k]); });
    return el;
  }

  function initState() {
    TEETH_SUPERIOR.forEach(function (t) { state.superior[t] = createEmptyToothData(); });
    TEETH_INFERIOR.forEach(function (t) { state.inferior[t] = createEmptyToothData(); });
  }

  function mergeLoaded(data) {
    if (!data || typeof data !== 'object') return;
    if (data.superior && typeof data.superior === 'object') {
      Object.keys(data.superior).forEach(function (k) {
        var t = parseInt(k, 10);
        if (state.superior[t]) state.superior[t] = Object.assign(createEmptyToothData(), data.superior[k]);
      });
    }
    if (data.inferior && typeof data.inferior === 'object') {
      Object.keys(data.inferior).forEach(function (k) {
        var t = parseInt(k, 10);
        if (state.inferior[t]) state.inferior[t] = Object.assign(createEmptyToothData(), data.inferior[k]);
      });
    }
  }

  function toothSVG(toothId, isImplant) {
    var cls = isImplant ? ' tooth-svg implant-mark' : ' tooth-svg';
    return '<svg class="' + cls + '" viewBox="0 0 24 28" xmlns="http://www.w3.org/2000/svg"><rect x="2" y="2" width="20" height="24" rx="3" ry="2" fill="#f5f5f5" stroke="#999" stroke-width="1"/>' +
      (isImplant ? '<circle cx="12" cy="8" r="3" fill="none" stroke="#1976d2" stroke-width="1.5"/>' : '') + '</svg>';
  }

  function renderHeader(table, teeth) {
    var thead = table.querySelector('thead');
    if (!thead) { thead = createEl('thead'); table.appendChild(thead); }
    thead.innerHTML = '';
    var tr = createEl('tr');
    tr.appendChild(createEl('th', 'param-cell', { scope: 'row' }));
    for (var i = 0; i < teeth.length; i++) {
      if (i === 8) {
        var sep = createEl('th', 'hemi-sep');
        sep.setAttribute('data-hemi', 'sep');
        tr.appendChild(sep);
      }
      var th = createEl('th', 'tooth-col');
      th.setAttribute('data-tooth', String(teeth[i]));
      th.textContent = teeth[i];
      tr.appendChild(th);
    }
    thead.appendChild(tr);
  }

  function wrapInCeldaDiente(td, content) {
    var celda = createEl('div', 'celda-diente');
    if (content) celda.appendChild(content);
    td.appendChild(celda);
  }

  function createCell(rowDef, tooth, arcade, baseTabIndex) {
    var data = state[arcade][tooth];
    var isDisabled = data.ausencia && rowDef.key !== 'ausencia';
    var td = createEl('td', 'tooth-col');
    td.setAttribute('data-tooth', String(tooth));
    td.setAttribute('data-row', rowDef.id);
    var tab = baseTabIndex == null ? 0 : baseTabIndex;

    if (rowDef.type === 'checkbox') {
      var cb = createEl('input', null, { type: 'checkbox', 'data-tooth': String(tooth), 'data-row': rowDef.id, tabindex: String(tab) });
      cb.checked = !!data[rowDef.key];
      if (rowDef.key === 'implante') cb.disabled = isDisabled;
      wrapInCeldaDiente(td, cb);
    } else if (rowDef.type === 'select') {
      var sel = createEl('select', null, { 'data-tooth': String(tooth), 'data-row': rowDef.id, tabindex: String(tab) });
      rowDef.options.forEach(function (opt) {
        var o = createEl('option', null, { value: opt });
        o.textContent = opt;
        sel.appendChild(o);
      });
      sel.value = data[rowDef.key] || rowDef.options[0];
      if (isDisabled) sel.disabled = true;
      wrapInCeldaDiente(td, sel);
    } else if (rowDef.type === 'miniMCD') {
      var celda = createEl('div', 'celda-diente');
      var label = createEl('span', 'celda-diente-label');
      label.textContent = 'M C D';
      celda.appendChild(label);
      var mcd = createEl('div', 'mcd');
      SITES.forEach(function (site, idx) {
        var cell = createEl('div', 'mini-cell' + (data[rowDef.key][site] ? ' active' : ''));
        cell.setAttribute('data-tooth', String(tooth));
        cell.setAttribute('data-row', rowDef.id);
        cell.setAttribute('data-site', site);
        cell.setAttribute('tabindex', String(tab + idx));
        cell.textContent = site;
        if (isDisabled) cell.style.pointerEvents = 'none';
        mcd.appendChild(cell);
      });
      celda.appendChild(mcd);
      td.appendChild(celda);
    } else if (rowDef.type === 'number') {
      var num = createEl('input', null, { type: 'number', min: rowDef.min, max: rowDef.max, 'data-tooth': String(tooth), 'data-row': rowDef.id, tabindex: String(tab) });
      num.value = data[rowDef.key] !== '' && data[rowDef.key] !== undefined ? data[rowDef.key] : '';
      num.placeholder = '0-' + rowDef.max;
      if (isDisabled) num.disabled = true;
      wrapInCeldaDiente(td, num);
    } else if (rowDef.type === 'tripleNumber') {
      td.classList.add('td-mcd');
      var celda = createEl('div', 'celda-diente');
      var label = createEl('span', 'celda-diente-label');
      label.textContent = 'M C D';
      celda.appendChild(label);
      var mcd = createEl('div', 'mcd');
      var allowNegative = rowDef.min < 0;
      SITES.forEach(function (site, idx) {
        var input = createEl('input', null, {
          type: 'text',
          inputmode: 'numeric',
          'data-tooth': String(tooth),
          'data-row': rowDef.id,
          'data-site': site,
          'data-allow-negative': allowNegative ? '1' : '0',
          tabindex: String(tab + idx),
          class: 'mcd-input'
        });
        var val = data[rowDef.key][site];
        input.value = val !== '' && val !== undefined ? String(val) : '';
        input.placeholder = site;
        if (isDisabled) input.disabled = true;
        mcd.appendChild(input);
      });
      celda.appendChild(mcd);
      td.appendChild(celda);
    } else if (rowDef.type === 'teethDraw') {
      var celda = createEl('div', 'celda-diente');
      var wrap = createEl('div', 'tooth-svg-wrap');
      wrap.innerHTML = toothSVG(tooth, !!data.implante);
      celda.appendChild(wrap);
      td.classList.add('row-teeth-draw');
      td.appendChild(celda);
    }
    return td;
  }

  function renderRow(table, rowDef, teeth, arcade) {
    var tbody = table.querySelector('tbody');
    if (!tbody) { tbody = createEl('tbody'); table.appendChild(tbody); }
    if (rowDef.sectionLabel) {
      var labelTr = createEl('tr', 'face-label-row');
      var labelTh = createEl('th', 'param-cell', { scope: 'row' });
      labelTh.textContent = rowDef.sectionLabel;
      labelTr.appendChild(labelTh);
      for (var k = 0; k < teeth.length; k++) {
        if (k === 8) {
          var sep = createEl('td', 'hemi-sep');
          sep.setAttribute('data-hemi', 'sep');
          labelTr.appendChild(sep);
        }
        var emptyTd = createEl('td', 'tooth-col');
        emptyTd.setAttribute('data-tooth', String(teeth[k]));
        labelTr.appendChild(emptyTd);
      }
      tbody.appendChild(labelTr);
    }
    var tr = createEl('tr');
    if (rowDef.group) tr.classList.add('row-group-' + rowDef.group);
    if (rowDef.groupEnd) tr.classList.add('row-group-end');
    var th = createEl('th', 'param-cell', { scope: 'row' });
    th.textContent = rowDef.label || '';
    tr.appendChild(th);
    var rowIndex = ROWS.indexOf(rowDef);
    for (var i = 0; i < teeth.length; i++) {
      if (i === 8) {
        var sep = createEl('td', 'hemi-sep');
        sep.setAttribute('data-hemi', 'sep');
        tr.appendChild(sep);
      }
      var baseTab = i * 100 + rowIndex;
      var td = createCell(rowDef, teeth[i], arcade, baseTab);
      if (i === 0) td.classList.add('first-tooth-col');
      tr.appendChild(td);
    }
    tbody.appendChild(tr);
  }

  function renderTable(containerId, teeth, arcade) {
    var container = byId(containerId);
    if (!container) return;
    container.innerHTML = '';
    var table = createEl('table');
    container.appendChild(table);
    renderHeader(table, teeth);
    ROWS.forEach(function (row) { renderRow(table, row, teeth, arcade); });
    teeth.forEach(function (t) {
      if (state[arcade][t].ausencia) {
        table.querySelectorAll('th[data-tooth="' + t + '"], td[data-tooth="' + t + '"]').forEach(function (cell) {
          cell.classList.add('disabled-by-absence');
        });
      }
    });
    attachCellListeners(table, teeth, arcade);
  }

  var dataRowSondajeV = 'sondajeVestibular';
  var dataRowSondajeP = 'sondajePalatal';
  var dataRowMargenV = 'margenVestibular';
  var dataRowMargenP = 'margenPalatal';

  function applyValidationClasses(input, row, site, data) {
    var td = input.closest('td');
    if (!td) return;
    td.classList.remove('cell-depth-warning', 'cell-depth-danger', 'cell-margin-negative');
    if (row === dataRowSondajeV || row === dataRowSondajeP) {
      var val = data[row][site];
      var num = parseInt(val, 10);
      if (val !== '' && !isNaN(num)) {
        if (num >= 6) td.classList.add('cell-depth-danger');
        else if (num >= 4) td.classList.add('cell-depth-warning');
      }
    } else if (row === dataRowMargenV || row === dataRowMargenP) {
      var v = data[row][site];
      var n = parseInt(v, 10);
      if (v !== '' && !isNaN(n) && n < 0) td.classList.add('cell-margin-negative');
    }
  }

  function attachCellListeners(table, teeth, arcade) {
    function handleInputChange(e) {
      var t = e.target;
      var tooth = t.getAttribute('data-tooth');
      var row = t.getAttribute('data-row');
      var site = t.getAttribute('data-site');
      if (!tooth || !row) return;
      var d = state[arcade][tooth];
      tooth = parseInt(tooth, 10);
      if (t.type === 'checkbox') {
        d[row] = t.checked;
        if (row === 'ausencia' || row === 'implante') {
          var cid = arcade === 'superior' ? 'tableSuperior' : 'tableInferior';
          renderTable(cid, teeth, arcade);
          updateSummary();
          return;
        }
      } else if (t.tagName === 'SELECT') {
        d[row] = t.value;
      } else if ((t.type === 'number' || t.classList.contains('mcd-input')) && site) {
        var raw = String(t.value).trim().replace(/[^0-9\-]/g, '');
        if (raw === '') { d[row][site] = ''; t.value = ''; }
        else if (raw === '-') {
          d[row][site] = '';
          if (row === dataRowMargenV || row === dataRowMargenP) t.value = '-';
          else t.value = '';
        } else {
          var num = parseInt(raw, 10);
          if (row === dataRowSondajeV || row === dataRowSondajeP) {
            num = isNaN(num) ? '' : Math.max(0, Math.min(12, num));
            d[row][site] = num === '' ? '' : num;
            t.value = num === '' ? '' : String(num);
          } else if (row === dataRowMargenV || row === dataRowMargenP) {
            num = isNaN(num) ? '' : Math.max(-9, Math.min(9, num));
            d[row][site] = num === '' ? '' : num;
            t.value = num === '' ? '' : String(num);
          }
        }
        applyValidationClasses(t, row, site, d);
      } else if (t.type === 'number' && !site) {
        var rowDef = ROWS.find(function (r) { return r.id === row; });
        var n = parseInt(t.value, 10);
        d[row] = t.value === '' ? '' : (isNaN(n) ? '' : Math.max(rowDef.min, Math.min(rowDef.max, n)));
      }
      updateSummary();
    }
    table.addEventListener('change', handleInputChange);
    table.addEventListener('input', function (e) {
      var t = e.target;
      if (t.type === 'number' || t.classList.contains('mcd-input')) {
        var tooth = t.getAttribute('data-tooth');
        var row = t.getAttribute('data-row');
        var site = t.getAttribute('data-site');
        if (tooth && row && site && state[arcade][tooth]) {
          var d = state[arcade][tooth];
          var raw = String(t.value).trim().replace(/[^0-9\-]/g, '');
          if (raw === '') d[row][site] = '';
          else if (raw === '-') {
            d[row][site] = '';
            if (row !== dataRowSondajeV && row !== dataRowSondajeP) t.value = '-';
            else t.value = '';
          } else {
            var num = parseInt(raw, 10);
            if (row === dataRowSondajeV || row === dataRowSondajeP) {
              num = isNaN(num) ? '' : Math.max(0, Math.min(12, num));
              d[row][site] = num === '' ? '' : num;
              t.value = num === '' ? '' : String(num);
            } else if (row === dataRowMargenV || row === dataRowMargenP) {
              num = isNaN(num) ? '' : Math.max(-9, Math.min(9, num));
              d[row][site] = num === '' ? '' : num;
              t.value = num === '' ? '' : String(num);
            }
          }
          applyValidationClasses(t, row, site, d);
        }
      }
    });
    function toggleMiniCell(cell) {
      var tooth = cell.getAttribute('data-tooth');
      var row = cell.getAttribute('data-row');
      var site = cell.getAttribute('data-site');
      if (!tooth || !row) return;
      var d = state[arcade][tooth];
      d[row][site] = !d[row][site];
      cell.classList.toggle('active', d[row][site]);
      updateSummary();
    }
    table.addEventListener('click', function (e) {
      var cell = e.target.closest('.mini-cell');
      if (!cell) return;
      e.preventDefault();
      toggleMiniCell(cell);
    });
    table.addEventListener('keydown', function (e) {
      var cell = e.target.closest('.mini-cell');
      if (!cell || cell.getAttribute('tabindex') == null) return;
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleMiniCell(cell); }
    });
    table.querySelectorAll('input[data-row="' + dataRowSondajeV + '"], input[data-row="' + dataRowSondajeP + '"]').forEach(function (input) {
      var tooth = input.getAttribute('data-tooth');
      var site = input.getAttribute('data-site');
      var d = state[arcade][tooth];
      if (d) applyValidationClasses(input, input.getAttribute('data-row'), site, d);
    });
    table.querySelectorAll('input[data-row="' + dataRowMargenV + '"], input[data-row="' + dataRowMargenP + '"]').forEach(function (input) {
      var tooth = input.getAttribute('data-tooth');
      var site = input.getAttribute('data-site');
      var d = state[arcade][tooth];
      if (d) applyValidationClasses(input, input.getAttribute('data-row'), site, d);
    });
  }

  function updateSummary() {
    var sangrado = 0, placa = 0, bolsillos4 = 0, bolsillos6 = 0, ausentes = 0, implantes = 0;
    var arcades = [{ arc: 'superior', teeth: TEETH_SUPERIOR }, { arc: 'inferior', teeth: TEETH_INFERIOR }];
    arcades.forEach(function (a) {
      a.teeth.forEach(function (t) {
        var d = state[a.arc][t];
        if (d.ausencia) ausentes++;
        if (d.implante) implantes++;
        SITES.forEach(function (site) {
          if (d.sangrado[site]) sangrado++;
          if (d.placa[site]) placa++;
          var sV = parseInt(d.sondajeVestibular[site], 10);
          var sP = parseInt(d.sondajePalatal[site], 10);
          if (!isNaN(sV) && sV >= 4) bolsillos4++;
          if (!isNaN(sV) && sV >= 6) bolsillos6++;
          if (!isNaN(sP) && sP >= 4) bolsillos4++;
          if (!isNaN(sP) && sP >= 6) bolsillos6++;
        });
      });
    });
    var panel = byId('summaryPanel');
    if (panel) {
      var s = panel.querySelector('[data-metric="sangrado"] strong'); if (s) s.textContent = sangrado;
      s = panel.querySelector('[data-metric="placa"] strong'); if (s) s.textContent = placa;
      s = panel.querySelector('[data-metric="bolsillos4"] strong'); if (s) s.textContent = bolsillos4;
      s = panel.querySelector('[data-metric="bolsillos6"] strong'); if (s) s.textContent = bolsillos6;
      s = panel.querySelector('[data-metric="ausentes"] strong'); if (s) s.textContent = ausentes;
      s = panel.querySelector('[data-metric="implantes"] strong'); if (s) s.textContent = implantes;
    }
  }

  function saveToServer() {
    var pacienteIdEl = byId('pacienteIdPeriodonto');
    if (!pacienteIdEl || !pacienteIdEl.value) return;
    var pacienteIdRaw = parseInt(pacienteIdEl.value, 10);
    if (isNaN(pacienteIdRaw) || pacienteIdRaw <= 0) return;
    var msg = byId('guardarMsg');
    if (msg) msg.textContent = 'Guardando...';
    var citaIdEl = byId('citaIdPeriodonto');
    var citaIdRaw = citaIdEl && citaIdEl.value ? parseInt(citaIdEl.value, 10) : NaN;
    var citaId = (typeof citaIdRaw === 'number' && !isNaN(citaIdRaw) && citaIdRaw > 0) ? citaIdRaw : null;
    var payload = {
      PacienteId: pacienteIdRaw,
      EstadoJson: JSON.stringify({ superior: state.superior, inferior: state.inferior }),
      CitaId: citaId
    };
    fetch('/Clinica/Expediente/GuardarPeriodontograma', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).then(function (r) {
      if (msg) msg.textContent = r.ok ? 'Guardado.' : 'Error al guardar.';
    }).catch(function () {
      if (msg) msg.textContent = 'Error de conexión.';
    });
  }

  function loadFromServer() {
    var pacienteIdEl = byId('pacienteIdPeriodonto');
    if (!pacienteIdEl || !pacienteIdEl.value) return;
    fetch('/Clinica/Expediente/GetPeriodontogramaJson?pacienteId=' + encodeURIComponent(pacienteIdEl.value), { headers: { 'Accept': 'application/json' } })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        if (data && (data.superior || data.inferior)) {
          mergeLoaded(data);
          renderTable('tableSuperior', TEETH_SUPERIOR, 'superior');
          renderTable('tableInferior', TEETH_INFERIOR, 'inferior');
          updateSummary();
        }
      })
      .catch(function () {});
  }

  function clearAll() {
    if (typeof confirm !== 'undefined' && !confirm('¿Limpiar todo el periodontograma?')) return;
    initState();
    renderTable('tableSuperior', TEETH_SUPERIOR, 'superior');
    renderTable('tableInferior', TEETH_INFERIOR, 'inferior');
    updateSummary();
  }

  function exportJSON() {
    var payload = { version: 1, exportDate: new Date().toISOString(), periodontograma: { superior: state.superior, inferior: state.inferior } };
    var blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    var a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'periodontograma_' + new Date().toISOString().slice(0, 10) + '.json';
    a.click();
    URL.revokeObjectURL(a.href);
  }

  function importJSON(file) {
    if (!file) return;
    var reader = new FileReader();
    reader.onload = function () {
      try {
        var parsed = JSON.parse(reader.result);
        var data = parsed.periodontograma || parsed;
        if (data.superior) mergeLoaded(data);
        if (data.inferior) mergeLoaded(data);
        renderTable('tableSuperior', TEETH_SUPERIOR, 'superior');
        renderTable('tableInferior', TEETH_INFERIOR, 'inferior');
        updateSummary();
        var msg = byId('guardarMsg');
        if (msg) msg.textContent = 'Importado.';
      } catch (err) {
        alert('Error al leer el JSON.');
      }
    };
    reader.readAsText(file);
  }

  function init() {
    var loadingEl = byId('periodontograma-loading');
    var containerSup = byId('tableSuperior');
    var containerInf = byId('tableInferior');
    if (!containerSup || !containerInf) {
      if (loadingEl) { loadingEl.textContent = 'No se encontraron los contenedores. Recargue (F5).'; loadingEl.style.color = '#c00'; }
      return;
    }
    if (loadingEl) loadingEl.style.display = 'none';

    initState();
    var estadoInicial = {};
    try {
      var dataEl = byId('periodontograma-data');
      if (dataEl && dataEl.textContent) estadoInicial = JSON.parse(dataEl.textContent);
    } catch (e) {}
    if (estadoInicial && (estadoInicial.superior || estadoInicial.inferior)) {
      mergeLoaded(estadoInicial);
    }

    renderTable('tableSuperior', TEETH_SUPERIOR, 'superior');
    renderTable('tableInferior', TEETH_INFERIOR, 'inferior');
    updateSummary();

    var btnSave = byId('btnSave');
    if (btnSave) btnSave.addEventListener('click', saveToServer);
    var btnClear = byId('btnClear');
    if (btnClear) btnClear.addEventListener('click', clearAll);
    var btnExport = byId('btnExport');
    if (btnExport) btnExport.addEventListener('click', exportJSON);
    var inputImport = byId('inputImport');
    if (inputImport) inputImport.addEventListener('change', function (e) {
      var f = e.target.files && e.target.files[0];
      importJSON(f);
      e.target.value = '';
    });

    if (!estadoInicial || (!estadoInicial.superior && !estadoInicial.inferior)) {
      loadFromServer();
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
