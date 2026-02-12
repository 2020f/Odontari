/**
 * OdontogramaPro - Odontograma digital interactivo según odontogramapro.mdc
 * 32 dientes FDI, superficies y estados de diente completo
 */
(function () {
  const FDI_SUPERIOR = [
    [18, 17, 16, 15, 14, 13, 12, 11], // Q1 der paciente
    [21, 22, 23, 24, 25, 26, 27, 28]  // Q2 izq paciente
  ];
  const FDI_INFERIOR = [
    [48, 47, 46, 45, 44, 43, 42, 41], // Q4 der paciente
    [31, 32, 33, 34, 35, 36, 37, 38]  // Q3 izq paciente
  ];
  const SUPERFICIES = ['oclusal', 'vestibular', 'palatino', 'mesial', 'distal'];

  function toothDefault() {
    const surfaces = {};
    SUPERFICIES.forEach(s => surfaces[s] = 'NONE');
    return { surfaces, status: 'NONE', notes: '' };
  }

  function initTeeth() {
    const teeth = {};
    FDI_SUPERIOR.flat().concat(FDI_INFERIOR.flat()).forEach(num => {
      teeth[String(num)] = toothDefault();
    });
    return teeth;
  }

  function loadState() {
    try {
      const raw = document.getElementById('estadoInicial').value || '{}';
      const data = JSON.parse(raw);
      if (data.teeth && typeof data.teeth === 'object') {
        const base = initTeeth();
        Object.keys(data.teeth).forEach(k => {
          if (base[k]) {
            base[k] = { ...toothDefault(), ...data.teeth[k] };
            if (!base[k].surfaces) base[k].surfaces = { ...toothDefault().surfaces };
          }
        });
        return { teeth: base, observations: data.observations || '' };
      }
    } catch (e) {}
    return { teeth: initTeeth(), observations: '' };
  }

  let state = loadState();
  let modoSuperficie = false;
  let estadoSuperficie = 'NONE';
  let estadoDiente = 'NONE';

  const svg = document.getElementById('odontogramaSvg');
  if (!svg) return;
  const TW = 52, TH = 44, GAP = 5;

  function addSurfaceRect(parent, x, y, w, h, surfName, toothNum, toothData) {
    const val = (toothData.surfaces && toothData.surfaces[surfName]) || 'NONE';
    const r = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    r.setAttribute('x', x); r.setAttribute('y', y); r.setAttribute('width', w); r.setAttribute('height', h);
    r.setAttribute('class', 'odontograma-surface surf-' + val);
    r.setAttribute('data-tooth', toothNum); r.setAttribute('data-surface', surfName);
    r.setAttribute('stroke', '#94a3b8'); r.setAttribute('stroke-width', '0.8');
    r.addEventListener('click', (e) => {
      e.stopPropagation();
      if (toothData.status === 'AUSENTE' || toothData.status === 'EXTRAIDO') return;
      if (modoSuperficie) {
        if (!toothData.surfaces) toothData.surfaces = toothDefault().surfaces;
        toothData.surfaces[surfName] = estadoSuperficie;
      } else {
        toothData.status = estadoDiente;
        if (estadoDiente === 'AUSENTE' || estadoDiente === 'EXTRAIDO') toothData.surfaces = toothDefault().surfaces;
      }
      render();
    });
    parent.appendChild(r);
  }

  function renderTooth(g, toothNum, toothData, cx, cy, isSuperior) {
    const blocked = toothData.status === 'AUSENTE' || toothData.status === 'EXTRAIDO';
    const tg = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    tg.setAttribute('class', 'odontograma-tooth' + (blocked ? ' surface-edit-disabled' : ''));
    tg.setAttribute('data-tooth', toothNum);

    const x0 = cx - TW / 2, y0 = cy - TH / 2, pad = 1;
    const rw = TW - 2 * pad, rh = TH - 2 * pad;
    const ch = rh / 3, cw = rw / 3;

    if (!blocked) {
      const border = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      border.setAttribute('x', x0); border.setAttribute('y', y0); border.setAttribute('width', TW); border.setAttribute('height', TH);
      border.setAttribute('fill', 'none'); border.setAttribute('stroke', '#64748b'); border.setAttribute('stroke-width', '1');
      tg.appendChild(border);
      addSurfaceRect(tg, x0 + pad, y0 + pad, rw, ch, 'vestibular', toothNum, toothData);
      addSurfaceRect(tg, x0 + pad, y0 + pad + ch, cw, ch, 'mesial', toothNum, toothData);
      addSurfaceRect(tg, x0 + pad + cw, y0 + pad + ch, cw, ch, 'oclusal', toothNum, toothData);
      addSurfaceRect(tg, x0 + pad + cw * 2, y0 + pad + ch, cw, ch, 'distal', toothNum, toothData);
      addSurfaceRect(tg, x0 + pad, y0 + pad + ch * 2, rw, ch, 'palatino', toothNum, toothData);
    } else {
      const fullRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      fullRect.setAttribute('x', x0); fullRect.setAttribute('y', y0); fullRect.setAttribute('width', TW); fullRect.setAttribute('height', TH);
      fullRect.setAttribute('class', 'tooth-' + (toothData.status || 'NONE'));
      fullRect.setAttribute('stroke', '#64748b'); fullRect.setAttribute('stroke-width', '1');
      tg.appendChild(fullRect);
      const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
      line.setAttribute('x1', cx - TW / 3); line.setAttribute('y1', cy); line.setAttribute('x2', cx + TW / 3); line.setAttribute('y2', cy);
      line.setAttribute('stroke', '#334155'); line.setAttribute('stroke-width', '1');
      tg.appendChild(line);
      if (toothData.status === 'EXTRAIDO') {
        const l2 = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        l2.setAttribute('x1', cx - TW / 4); l2.setAttribute('y1', cy - TH / 4); l2.setAttribute('x2', cx + TW / 4); l2.setAttribute('y2', cy + TH / 4);
        l2.setAttribute('stroke', '#B71C1C'); l2.setAttribute('stroke-width', '1.5');
        tg.appendChild(l2);
        const l3 = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        l3.setAttribute('x1', cx + TW / 4); l3.setAttribute('y1', cy - TH / 4); l3.setAttribute('x2', cx - TW / 4); l3.setAttribute('y2', cy + TH / 4);
        l3.setAttribute('stroke', '#B71C1C'); l3.setAttribute('stroke-width', '1.5');
        tg.appendChild(l3);
      }
    }

    const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    text.setAttribute('x', cx); text.setAttribute('text-anchor', 'middle'); text.setAttribute('class', 'odontograma-fdi');
    text.setAttribute('font-size', '10'); text.setAttribute('fill', '#1e40af');
    text.textContent = toothNum;
    text.setAttribute('y', isSuperior ? y0 - 2 : y0 + TH + 12);
    tg.appendChild(text);

    g.appendChild(tg);
  }

  function renderRow(g, row, cy, isSuperior) {
    const nums = row;
    const totalW = nums.length * (TW + GAP) - GAP;
    let sx = 400 - totalW / 2;
    nums.forEach(num => {
      const toothData = state.teeth[String(num)] || toothDefault();
      state.teeth[String(num)] = toothData;
      renderTooth(g, num, toothData, sx + TW / 2, cy, isSuperior);
      sx += TW + GAP;
    });
  }

  function render() {
    while (svg.firstChild) svg.removeChild(svg.firstChild);
    const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    const midline = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    midline.setAttribute('x1', 400); midline.setAttribute('y1', 55); midline.setAttribute('x2', 400); midline.setAttribute('y2', 365);
    midline.setAttribute('stroke', '#94a3b8'); midline.setAttribute('stroke-width', '0.5'); midline.setAttribute('stroke-dasharray', '4 3');
    g.appendChild(midline);
    renderRow(g, FDI_SUPERIOR[0], 82, true);
    renderRow(g, FDI_SUPERIOR[1], 134, true);
    renderRow(g, FDI_INFERIOR[0], 294, false);
    renderRow(g, FDI_INFERIOR[1], 346, false);

    const labelSup = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    labelSup.setAttribute('x', 50);
    labelSup.setAttribute('y', 105);
    labelSup.setAttribute('font-size', '12');
    labelSup.setAttribute('fill', '#666');
    labelSup.textContent = 'Arcada superior';
    g.appendChild(labelSup);

    const labelInf = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    labelInf.setAttribute('x', 50);
    labelInf.setAttribute('y', 315);
    labelInf.setAttribute('font-size', '12');
    labelInf.setAttribute('fill', '#666');
    labelInf.textContent = 'Arcada inferior';
    g.appendChild(labelInf);

    svg.appendChild(g);
    if (typeof updateHallazgosYStats === 'function') updateHallazgosYStats();
  }

  function updatePanelVisibility() {
    const ps = document.getElementById('panelSuperficie');
    const pd = document.getElementById('panelDiente');
    const lb = document.getElementById('labelEstados');
    const btnSup = document.getElementById('btnModoSuperficie');
    const btnDen = document.getElementById('btnModoDiente');
    if (ps) ps.style.display = modoSuperficie ? 'block' : 'none';
    if (pd) pd.style.display = modoSuperficie ? 'none' : 'block';
    if (lb) lb.textContent = modoSuperficie ? 'ESTADOS DE SUPERFICIE' : 'ESTADOS DE DIENTE';
    if (btnSup) btnSup.classList.toggle('active', modoSuperficie);
    if (btnDen) btnDen.classList.toggle('active', !modoSuperficie);
  }

  function updateHallazgosYStats() {
    const hallazgos = [];
    const dientesAfectados = new Set();
    Object.entries(state.teeth).forEach(([num, t]) => {
      if (t.status && t.status !== 'NONE') {
        hallazgos.push('Diente ' + num + ': ' + t.status);
        dientesAfectados.add(num);
      } else if (t.surfaces) {
        Object.entries(t.surfaces).forEach(([s, v]) => {
          if (v && v !== 'NONE') {
            hallazgos.push('Diente ' + num + ' (' + s + '): ' + v);
            dientesAfectados.add(num);
          }
        });
      }
    });
    const hallazgosEl = document.getElementById('hallazgos');
    const statH = document.getElementById('statHallazgos');
    const statD = document.getElementById('statDientes');
    if (hallazgosEl) {
      hallazgosEl.textContent = '';
      if (hallazgos.length) {
        hallazgos.forEach(h => {
          const d = document.createElement('div');
          d.className = 'hallazgo-item';
          d.textContent = h;
          hallazgosEl.appendChild(d);
        });
      } else {
        hallazgosEl.textContent = 'Sin hallazgos registrados.';
      }
    }
    if (statH) statH.textContent = hallazgos.length;
    if (statD) statD.textContent = dientesAfectados.size;
  }

  document.getElementById('btnModoSuperficie')?.addEventListener('click', () => {
    modoSuperficie = true;
    updatePanelVisibility();
    render();
  });
  document.getElementById('btnModoDiente')?.addEventListener('click', () => {
    modoSuperficie = false;
    updatePanelVisibility();
    render();
  });

  const panelSup = document.getElementById('panelSuperficie');
  if (panelSup) {
    (panelSup.querySelectorAll('.btn-estado') || panelSup.querySelectorAll('.btn-state')).forEach(btn => {
      btn.addEventListener('click', () => {
        estadoSuperficie = btn.getAttribute('data-state') || 'NONE';
        panelSup.querySelectorAll('.btn-estado, .btn-state').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });
  }

  const panelDen = document.getElementById('panelDiente');
  if (panelDen) {
    (panelDen.querySelectorAll('.btn-estado') || panelDen.querySelectorAll('.btn-state')).forEach(btn => {
      btn.addEventListener('click', () => {
        estadoDiente = btn.getAttribute('data-state') || 'NONE';
        panelDen.querySelectorAll('.btn-estado, .btn-state').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });
  }

  const obsEl = document.getElementById('observaciones');
  if (obsEl) {
    obsEl.value = state.observations || '';
    obsEl.addEventListener('input', () => { state.observations = obsEl.value; });
  }

  document.getElementById('btnGuardar')?.addEventListener('click', () => {
    const pacienteId = parseInt(document.getElementById('pacienteId')?.value || '0', 10);
    if (!pacienteId) return;
    if (obsEl) state.observations = obsEl.value;
    const payload = JSON.stringify({
      PacienteId: pacienteId,
      EstadoJson: JSON.stringify({ teeth: state.teeth, observations: state.observations })
    });
    const msg = document.getElementById('guardarMsg');
    if (msg) msg.textContent = 'Guardando...';
    fetch('/Clinica/Expediente/GuardarOdontograma', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: payload
    })
      .then(r => {
        if (msg) msg.textContent = r.ok ? 'Guardado.' : 'Error al guardar.';
      })
      .catch(() => { if (msg) msg.textContent = 'Error de conexión.'; })
      .finally(() => { setTimeout(() => { if (msg) msg.textContent = ''; }, 3000); });
  });

  const origRender = render;
  render = function() {
    origRender();
    updateHallazgosYStats?.();
  };

  modoSuperficie = true;
  estadoSuperficie = 'CARIES';
  updatePanelVisibility();
  updateHallazgosYStats?.();
  render();
})();
