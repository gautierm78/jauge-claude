// ==UserScript==
// @name         Claude – jauge d'utilisation
// @namespace    madoe.fr
// @version      2.1
// @description  Affiche en permanence l'utilisation Claude (session 5 h + semaine) et alerte à 70 % / 90 %.
// @match        *://*/*
// @noframes
// @connect      claude.ai
// @connect      127.0.0.1
// @grant        GM_xmlhttpRequest
// @grant        GM_notification
// @grant        GM_getValue
// @grant        GM_setValue
// @grant        GM_addValueChangeListener
// @grant        GM_registerMenuCommand
// @run-at       document-idle
// ==/UserScript==

(function () {
  'use strict';

  // ---- Réglages -----------------------------------------------------------
  const SEUILS = [70, 90];          // alertes Windows à ces pourcentages
  const INTERVALLE_MIN = 5;         // fréquence de rafraîchissement (minutes)
  const URL_USAGE = 'https://claude.ai/settings/usage';
  // -------------------------------------------------------------------------

  const API = 'https://claude.ai/api';
  // La jauge ne s'affiche que sur claude.ai ; le reste (alertes, icône Windows) tourne sur tout site.
  const AFFICHER_ICI = location.hostname === 'claude.ai' || location.hostname.endsWith('.claude.ai');
  const K_DATA = 'usage_data', K_ORG = 'org_id', K_NOTIF = 'notified', K_EN_COURS = 'en_cours';

  function get(url) {
    return new Promise((resolve, reject) => {
      GM_xmlhttpRequest({
        method: 'GET', url, anonymous: false,
        headers: { 'Accept': 'application/json' },
        onload: r => (r.status === 200 ? resolve(JSON.parse(r.responseText)) : reject(new Error('HTTP ' + r.status))),
        onerror: () => reject(new Error('réseau')),
      });
    });
  }

  const utile = u => u && (u.five_hour || u.seven_day);

  // Plusieurs organisations possibles (perso, équipe, API) : on garde celle qui a des limites.
  async function fetchUsage() {
    const id = GM_getValue(K_ORG, null);
    if (id) {
      try { const u = await get(`${API}/organizations/${id}/usage`); if (utile(u)) return u; } catch (e) { /* on recherche à nouveau */ }
    }
    const orgs = await get(API + '/organizations');
    for (const o of orgs.filter(o => (o.capabilities || []).includes('chat'))) {
      try {
        const u = await get(`${API}/organizations/${o.uuid}/usage`);
        if (utile(u)) { GM_setValue(K_ORG, o.uuid); return u; }
      } catch (e) { /* org suivante */ }
    }
    throw new Error('aucune limite trouvée');
  }

  async function refresh(force) {
    const cache = GM_getValue(K_DATA, null);
    if (!force && cache && Date.now() - cache.at < INTERVALLE_MIN * 60e3 - 5e3) return render(cache);
    // Un seul onglet interroge claude.ai à la fois (évite les doublons d'alertes).
    if (!force && Date.now() - GM_getValue(K_EN_COURS, 0) < 30e3) return cache && render(cache);
    GM_setValue(K_EN_COURS, Date.now());
    try {
      const u = await fetchUsage();
      const data = {
        at: Date.now(), error: null,
        session: u.five_hour && { pct: Math.round(u.five_hour.utilization), reset: u.five_hour.resets_at },
        semaine: u.seven_day && { pct: Math.round(u.seven_day.utilization), reset: u.seven_day.resets_at },
      };
      GM_setValue(K_DATA, data);   // partagé avec les autres onglets
      alertes(data);
      envoyerIcone(data);
      render(data);
    } catch (e) {
      render({ ...(cache || {}), error: e.message });
    }
  }

  // Transmet les chiffres à l'icône Windows (programme jauge_claude.pyw), si elle tourne.
  function envoyerIcone(d, verbeux) {
    const fin = msg => verbeux && GM_notification({ title: 'Jauge Claude : test de l\'icône Windows', text: msg, timeout: 8000 });
    GM_xmlhttpRequest({
      method: 'POST', url: 'http://127.0.0.1:47321/usage', timeout: 3000,
      headers: { 'Content-Type': 'application/json', 'X-Jauge-Claude': '1' },
      data: JSON.stringify({ session: d.session, semaine: d.semaine }),
      onload: r => fin(r.status === 204 ? 'OK : chiffres transmis à l\'icône.' : 'Réponse inattendue : HTTP ' + r.status),
      onerror: r => fin('Échec : ' + ((r && r.error) || 'connexion refusée ou bloquée') + '. L\'icône tourne-t-elle ? Accès 127.0.0.1 autorisé ?'),
      ontimeout: () => fin('Échec : pas de réponse en 3 s.'),
    });
  }

  function alertes(d) {
    const notif = GM_getValue(K_NOTIF, {});
    for (const [nom, v] of [['Session', d.session], ['Semaine', d.semaine]]) {
      if (!v) continue;
      for (const s of SEUILS) {
        // L'heure de remise à zéro renvoyée varie de quelques microsecondes à chaque requête :
        // on l'arrondit à 10 min pour reconnaître la même période.
        const periode = Math.round(Date.parse(v.reset) / 6e5);
        const cle = `${nom}|${periode}|${s}`;   // une seule alerte par seuil et par période
        if (v.pct >= s && !notif[cle]) {
          notif[cle] = Date.now();
          GM_setValue(K_NOTIF, notif);           // enregistré tout de suite, avant d'alerter
          GM_notification({
            title: `Claude : ${nom.toLowerCase()} à ${v.pct} %`,
            text: `Seuil de ${s} % dépassé. Remise à zéro ${quand(v.reset)}.`,
            timeout: 0,
            onclick: () => window.open(URL_USAGE, '_blank'),
          });
        }
      }
    }
    for (const k in notif) if (Date.now() - notif[k] > 8 * 864e5) delete notif[k]; // ménage
    GM_setValue(K_NOTIF, notif);
  }

  function quand(iso) {
    if (!iso) return '?';
    const d = new Date(iso), min = Math.round((d - Date.now()) / 60e3);
    if (min < 60) return `dans ${Math.max(min, 0)} min`;
    if (min < 24 * 60) return `dans ${Math.floor(min / 60)} h ${String(min % 60).padStart(2, '0')}`;
    return d.toLocaleString('fr-FR', { weekday: 'short', hour: '2-digit', minute: '2-digit' });
  }

  const couleur = p => (p >= 90 ? '#d64545' : p >= 70 ? '#e0a100' : '#2e9e5b');

  // ---- Affichage ----------------------------------------------------------
  let host;
  function render(d) {
    if (!AFFICHER_ICI) return;
    if (!host) {
      host = document.createElement('div');
      host.attachShadow({ mode: 'open' });
      document.documentElement.appendChild(host);
    }
    const ligne = (nom, v) => !v ? '' : `
      <div class="l" title="${nom} : remise à zéro ${quand(v.reset)}">
        <span class="n">${nom}</span>
        <span class="bar"><i style="width:${Math.min(v.pct, 100)}%;background:${couleur(v.pct)}"></i></span>
        <b style="color:${couleur(v.pct)}">${v.pct}%</b>
      </div>`;
    host.shadowRoot.innerHTML = `
      <style>
        .w{position:fixed;right:12px;bottom:12px;z-index:2147483647;font:12px/1.3 system-ui,sans-serif;
           background:rgba(30,30,30,.88);color:#eee;padding:6px 9px;border-radius:9px;cursor:pointer;
           box-shadow:0 2px 8px rgba(0,0,0,.3);opacity:.85;transition:opacity .2s;user-select:none}
        .w:hover{opacity:1}
        .l{display:flex;align-items:center;gap:6px;margin:2px 0}
        .n{width:54px;color:#bbb}
        .bar{width:70px;height:6px;background:#555;border-radius:3px;overflow:hidden}
        .bar i{display:block;height:100%}
        b{width:34px;text-align:right}
        .e{color:#f88;font-size:11px}
      </style>
      <div class="w" title="Clic : page Utilisation · Clic droit : actualiser">
        ${ligne('Session', d.session)}${ligne('Semaine', d.semaine)}
        ${d.error ? `<div class="e">⚠ ${d.error === 'HTTP 401' || d.error === 'HTTP 403' ? 'connecte-toi à claude.ai' : 'erreur : ' + d.error}</div>` : ''}
      </div>`;
    const w = host.shadowRoot.querySelector('.w');
    w.onclick = () => window.open(URL_USAGE, '_blank');
    w.oncontextmenu = e => { e.preventDefault(); refresh(true); };
  }

  GM_registerMenuCommand('Actualiser maintenant', () => refresh(true));
  GM_registerMenuCommand('Tester l\'icône Windows', () => {
    const d = GM_getValue(K_DATA, null);
    if (d && d.session) envoyerIcone(d, true);
    else GM_notification({ title: 'Jauge Claude', text: 'Pas encore de chiffres : ouvrez claude.ai puis réessayez.', timeout: 8000 });
  });
  GM_addValueChangeListener(K_DATA, (k, o, n, remote) => remote && n && render(n));

  refresh();
  setInterval(() => refresh(), 60e3);   // le cache partagé limite à 1 requête / 5 min
  document.addEventListener('visibilitychange', () => document.visibilityState === 'visible' && refresh());
})();
