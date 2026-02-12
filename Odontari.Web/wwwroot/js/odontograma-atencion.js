/**
 * OdontogramaPro - Vista Atención (Expediente de cita)
 * Mismos IDs con sufijo "At" para evitar conflictos
 */
(function () {
  const FDI_SUPERIOR = [[18,17,16,15,14,13,12,11],[21,22,23,24,25,26,27,28]];
  const FDI_INFERIOR = [[48,47,46,45,44,43,42,41],[31,32,33,34,35,36,37,38]];
  const SUPERFICIES = ['oclusal','vestibular','palatino','mesial','distal'];

  function toothDefault() {
    const surfaces = {};
    SUPERFICIES.forEach(s => surfaces[s] = 'NONE');
    return { surfaces, status: 'NONE', notes: '' };
  }

  function initTeeth() {
    const teeth = {};
    FDI_SUPERIOR.flat().concat(FDI_INFERIOR.flat()).forEach(num => { teeth[String(num)] = toothDefault(); });
    return teeth;
  }

  function loadState() {
    try {
      const el = document.getElementById('estadoInicialAt');
      if (!el) return { teeth: initTeeth(), observations: '' };
      const raw = el.value || '{}';
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

  const svgEl = document.getElementById('odontogramaSvgAt');
  if (!svgEl) return;

  let state = loadState();
  let modoSuperficie = false;
  let estadoSuperficie = 'NONE';
  let estadoDiente = 'NONE';

  const TW = 52, TH = 44, GAP = 5;

  function addSurfaceRect(parent, x, y, w, h, surfName, toothNum, toothData) {
    const val = (toothData.surfaces && toothData.surfaces[surfName]) || 'NONE';
    const r = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    r.setAttribute('x', x); r.setAttribute('y', y); r.setAttribute('width', w); r.setAttribute('height', h);
    r.setAttribute('class', 'odontograma-surface surf-' + val);
    r.setAttribute('data-tooth', toothNum); r.setAttribute('data-surface', surfName);
    r.setAttribute('stroke', '#94a3b8'); r.setAttribute('stroke-width', '0.5');
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

  function renderTooth(g, toothNum, toothData, cx, cy) {
    const blocked = toothData.status === 'AUSENTE' || toothData.status === 'EXTRAIDO';
    const tg = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    tg.setAttribute('class', 'odontograma-tooth' + (blocked ? ' surface-edit-disabled' : ''));
    tg.setAttribute('data-tooth', toothNum);

    const x0 = cx - TW / 2, y0 = cy - TH / 2, pad = 1;
    const rw = TW - 2 * pad, rh = TH - 2 * pad;
    const ch = rh / 3;
    const cw = rw / 3;

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
    text.setAttribute('x', cx); text.setAttribute('y', cy + 4);
    text.setAttribute('text-anchor', 'middle'); text.setAttribute('class', 'odontograma-fdi');
    text.setAttribute('font-size', '10');
    text.textContent = toothNum;
    tg.appendChild(text);

    g.appendChild(tg);
  }

  function renderRow(g, row, cy) {
    const totalW = row.length * (TW + GAP) - GAP;
    let sx = 400 - totalW / 2;
    row.forEach(num => {
      const toothData = state.teeth[String(num)] || toothDefault();
      state.teeth[String(num)] = toothData;
      renderTooth(g, num, toothData, sx + TW / 2, cy);
      sx += TW + GAP;
    });
  }

  function render() {
    while (svgEl.firstChild) svgEl.removeChild(svgEl.firstChild);
    const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    const midline = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    midline.setAttribute('x1', 400); midline.setAttribute('y1', 55); midline.setAttribute('x2', 400); midline.setAttribute('y2', 365);
    midline.setAttribute('stroke', '#94a3b8'); midline.setAttribute('stroke-width', '0.5'); midline.setAttribute('stroke-dasharray', '4 3');
    g.appendChild(midline);
    renderRow(g, FDI_SUPERIOR[0], 82);
    renderRow(g, FDI_SUPERIOR[1], 134);
    renderRow(g, FDI_INFERIOR[0], 294);
    renderRow(g, FDI_INFERIOR[1], 346);
    const ls = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    ls.setAttribute('x', 50); ls.setAttribute('y', 108); ls.setAttribute('font-size', '12'); ls.setAttribute('fill', '#64748b');
    ls.textContent = 'Arcada superior';
    g.appendChild(ls);
    const li = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    li.setAttribute('x', 50); li.setAttribute('y', 318); li.setAttribute('font-size', '12'); li.setAttribute('fill', '#64748b');
    li.textContent = 'Arcada inferior';
    g.appendChild(li);
    svgEl.appendChild(g);
    if (typeof updateHallazgosYStats === 'function') updateHallazgosYStats();
  }

  function updatePanelVisibility() {
    const ps = document.getElementById('panelAtSuperficie');
    const pd = document.getElementById('panelAtDiente');
    const lb = document.getElementById('labelEstadosAt');
    const btnSup = document.getElementById('btnModoAtSuperficie');
    const btnDen = document.getElementById('btnModoAtDiente');
    if (ps) ps.style.display = modoSuperficie ? 'block' : 'none';
    if (pd) pd.style.display = modoSuperficie ? 'none' : 'block';
    if (lb) lb.textContent = modoSuperficie ? 'Estados de superficie' : 'Estados de diente';
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
    const hallazgosEl = document.getElementById('hallazgosAt');
    const statH = document.getElementById('statHallazgosAt');
    const statD = document.getElementById('statDientesAt');
    if (hallazgosEl) hallazgosEl.textContent = hallazgos.length ? hallazgos.join('\n') : 'Sin hallazgos registrados.';
    if (statH) statH.textContent = hallazgos.length;
    if (statD) statD.textContent = dientesAfectados.size;
  }

  document.getElementById('btnModoAtSuperficie')?.addEventListener('click', () => {
    modoSuperficie = true;
    updatePanelVisibility();
    render();
  });
  document.getElementById('btnModoAtDiente')?.addEventListener('click', () => {
    modoSuperficie = false;
    updatePanelVisibility();
    render();
  });

  const panelSup = document.getElementById('panelAtSuperficie');
  if (panelSup) {
    panelSup.querySelectorAll('.btn-estado').forEach(btn => {
      btn.addEventListener('click', () => {
        estadoSuperficie = btn.getAttribute('data-state') || 'NONE';
        panelSup.querySelectorAll('.btn-estado').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });
  }

  const panelDen = document.getElementById('panelAtDiente');
  if (panelDen) {
    panelDen.querySelectorAll('.btn-estado').forEach(btn => {
      btn.addEventListener('click', () => {
        estadoDiente = btn.getAttribute('data-state') || 'NONE';
        panelDen.querySelectorAll('.btn-estado').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });
  }

  const obsEl = document.getElementById('observacionesAt');
  if (obsEl) {
    obsEl.value = state.observations || '';
    obsEl.addEventListener('input', () => { state.observations = obsEl.value; });
  }

  const btnGuardar = document.getElementById('btnGuardarAt');
  if (btnGuardar) {
    btnGuardar.addEventListener('click', () => {
      const pidEl = document.getElementById('pacienteIdAt');
      const pacienteId = pidEl ? parseInt(pidEl.value, 10) : 0;
      if (!pacienteId) return;
      if (obsEl) state.observations = obsEl.value;
      const payload = JSON.stringify({
        PacienteId: pacienteId,
        EstadoJson: JSON.stringify({ teeth: state.teeth, observations: state.observations })
      });
      const msg = document.getElementById('guardarMsgAt');
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
  }

  modoSuperficie = true;
  estadoSuperficie = 'CARIES';
  estadoDiente = 'NONE';
  updatePanelVisibility();
  updateHallazgosYStats();
  render();
})();
