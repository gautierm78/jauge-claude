# Jauge d'utilisation Claude – guide d'installation

Oct 24, 2026 · @gautier

## À quoi ça sert

Cette petite jauge affiche en permanence combien de votre quota Claude vous avez consommé, pour ne plus être bloqué par surprise. Elle vous prévient aussi par une notification Windows quand vous approchez de la limite.

Claude impose deux limites d'utilisation :

- **Session** : une fenêtre de 5 heures. Une fois pleine, il faut attendre sa remise à zéro.
- **Semaine** : un plafond sur 7 jours, qui repart à zéro à heure fixe.

La jauge prend la forme d'un petit cadre sombre en bas à droite de l'écran, avec deux barres : « Session » et « Semaine ». Elle s'installe en une dizaine de minutes, est gratuite et ne consomme rien de votre quota Claude.

En option, une pastille colorée dans la zone de notification Windows, près de l'horloge, peut remplacer la jauge du navigateur (voir la section **Option : l'icône Windows**). Tous les fichiers sont dans l'archive `JaugeClaude.zip` et, en texte, dans les annexes.

## Avant de commencer

Il vous faut :

- un ordinateur avec **Google Chrome** ou **Microsoft Edge** ;
- un compte Claude (Pro, Max, Team ou Enterprise) et être **connecté à claude.ai** dans ce navigateur ;
- le droit d'installer une extension (sur un PC professionnel verrouillé, demandez à votre service informatique) ;
- environ 10 minutes.

Aucune compétence technique n'est nécessaire : il suffit de copier-coller un texte.

## Installation pas à pas

L'installation tient en cinq étapes. Faites-les dans l'ordre, sans en sauter.

1. **Installer l'extension Tampermonkey.**
   - Ouvrez le magasin d'extensions de votre navigateur : Chrome Web Store pour Chrome, « Modules complémentaires Edge » pour Edge.
   - Cherchez « Tampermonkey », choisissez celle de l'éditeur Jan Biniok, puis cliquez sur **Ajouter**.
   - Cliquez sur l'icône en forme de pièce de puzzle en haut à droite, puis sur l'épingle à côté de Tampermonkey pour garder son icône visible.
2. **Autoriser les scripts.** Sans cette étape, rien ne fonctionnera.
   - Chrome : tapez `chrome://extensions` dans la barre d'adresse, cliquez sur **Détails** sous Tampermonkey et activez **Autoriser les scripts utilisateur**.
   - Edge : tapez `edge://extensions`, ouvrez les **Détails** de Tampermonkey et activez **Autoriser les scripts utilisateur**. Si l'option n'existe pas, activez le **Mode développeur** en bas à gauche de la page.
3. **Coller le script.**
   - Cliquez sur l'icône Tampermonkey, puis sur **Créer un nouveau script**.
   - Un éditeur s'ouvre avec quelques lignes d'exemple. Sélectionnez tout (Ctrl+A) et supprimez.
   - Copiez le code complet de l'annexe en fin de document et collez-le (Ctrl+V).
   - Enregistrez avec Ctrl+S. Le script « Claude – jauge d'utilisation » apparaît dans la liste, avec un interrupteur vert.
4. **Autoriser l'accès à claude.ai.**
   - Ouvrez [claude.ai](https://claude.ai) et rechargez la page (F5).
   - Tampermonkey peut ouvrir un nouvel onglet qui demande si le script peut accéder à claude.ai. Cliquez sur **Toujours autoriser le domaine**. Cet onglet s'ouvre parfois en arrière-plan : jetez un œil à vos onglets.
5. **Vérifier.** Rechargez claude.ai : la jauge apparaît en bas à droite avec vos deux pourcentages. Si Windows demande d'autoriser les notifications du navigateur, acceptez.

## Utilisation au quotidien

Vous n'avez rien à faire : la jauge se met à jour toute seule toutes les 5 minutes.

| Couleur | Niveau | Ce que ça veut dire |
| --- | --- | --- |
| Vert | moins de 70 % | Tout va bien. |
| Orange | de 70 à 89 % | Gardez les grosses tâches pour après la remise à zéro. |
| Rouge | 90 % et plus | Blocage proche : terminez l'essentiel. |

Une notification Windows arrive une seule fois à 70 % et une seule fois à 90 %, pour la session comme pour la semaine. Cliquer dessus ouvre la page d'utilisation de Claude.

**Les gestes utiles**

- **Survoler une barre** : affiche l'heure de remise à zéro.
- **Clic sur la jauge** : ouvre la page officielle Paramètres → Utilisation de Claude.
- **Clic droit sur la jauge** : actualise tout de suite.

**Choisir où la voir**

Par défaut, la jauge n'apparaît que sur claude.ai. Les alertes, elles, fonctionnent quel que soit le site ouvert. Cliquez sur l'icône Tampermonkey pour trouver deux commandes :

- **Afficher / masquer sur \[nom du site\]** : ajoute ou retire la jauge sur le site où vous êtes.
- **Afficher partout / seulement sur claude.ai** : bascule entre les deux modes pour tous les sites à la fois.

## Option : l'icône Windows

Une pastille ronde près de l'horloge affiche le pourcentage le plus élevé (session ou semaine). Elle est verte sous 70 %, orange de 70 à 89 %, rouge à partir de 90 %, et grise avec un « ? » sans nouvelles depuis 20 minutes. Au survol, elle affiche le détail et les heures de remise à zéro.

Le script Tampermonkey continue de lire les chiffres et les transmet à un petit programme local. **Chrome ou Edge doit donc rester ouvert**, même réduit.

**Installation**

1. Vérifiez que le script Tampermonkey est en **version 1.4** (colonne Version du tableau de bord). Sinon, mettez-le à jour avec le code de l'annexe A.
2. Installez **Python 3.12** depuis le Microsoft Store, s'il n'est pas déjà présent.
3. Extrayez `JaugeClaude.zip` (clic droit, **Extraire tout**) dans un dossier qui ne bougera plus, par exemple `C:\Outils\JaugeClaude`.
4. Double-cliquez sur `installer.bat`. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur **Informations complémentaires**, puis **Exécuter quand même**. Le programme s'installe, se lance et démarrera avec Windows.
5. Ouvrez claude.ai, puis lancez **Tester l'icône Windows** depuis le menu Tampermonkey. Si Tampermonkey demande l'accès à `127.0.0.1`, choisissez **Toujours autoriser le domaine**. La pastille prend sa couleur.
6. Si la pastille est cachée derrière la flèche ^, faites-la glisser dans la barre des tâches.

**Masquer la jauge du navigateur**

Sur claude.ai, icône Tampermonkey → **Afficher / masquer sur claude.ai**. Le script continue de tourner en arrière-plan : ne le désactivez pas, sinon la pastille repasse au gris. Les notifications à 70 % et 90 % restent actives.

**Dépannage de l'icône**

| Ce que vous voyez | Solution |
| --- | --- |
| Pastille grise « en attente du navigateur » | Lancez **Tester l'icône Windows** et lisez la notification. |
| Notification « Échec … » | Autorisez l'accès à `127.0.0.1` si Tampermonkey ou le navigateur le demande, puis refaites le test. |
| Pas de pastille du tout | Relancez `installer.bat`. Pour vérifier que le programme tourne, ouvrez `http://127.0.0.1:47321/usage` dans le navigateur : une page « 501 » signifie qu'il répond. |
| « Python n'est pas installé » | Installez Python 3.12 depuis le Microsoft Store, puis relancez `installer.bat`. |

**Désinstaller l'icône** : clic droit sur la pastille → **Quitter**. Tapez ensuite `shell:startup` dans la barre d'adresse de l'Explorateur et supprimez le raccourci « Jauge Claude ». Supprimez enfin le dossier `JaugeClaude`.

**Sécurité** : le programme n'écoute que sur l'ordinateur lui-même (`127.0.0.1`), jamais sur le réseau. Il n'accepte que les messages du script : un site web ne peut pas lui envoyer de faux chiffres.

## Comment ça marche

Le script pose à claude.ai la même question que la page officielle Paramètres → Utilisation, puis affiche la réponse.

&#91;embedded content: fonctionnement de la jauge · 3 acteurs, 2 résultats\]

- **Il utilise votre connexion existante.** Il ne contient ni mot de passe ni clé : il profite de votre session claude.ai déjà ouverte dans le navigateur.
- **Il ne consomme pas votre quota.** Il lit seulement un compteur, sans faire appel à l'IA.
- **Il reste discret.** Le résultat est partagé entre tous vos onglets et n'est redemandé qu'au bout de 5 minutes, et seulement si un onglet est au premier plan.
- **Rien ne sort de votre ordinateur.** Le script ne communique qu'avec claude.ai et n'envoie aucune donnée ailleurs.
- **Si vous avez plusieurs comptes** (personnel, équipe), il choisit automatiquement celui qui a des limites.

**Sa limite** : l'adresse interrogée est celle qu'utilise le site claude.ai pour sa propre page. Elle n'est pas documentée officiellement. Si Anthropic la modifie, la jauge affichera une erreur jusqu'à ce que le script soit adapté.

## Dépannage

La plupart des problèmes viennent d'une autorisation oubliée à l'étape 2 ou 4.

| Ce que vous voyez | Solution |
| --- | --- |
| Aucune jauge sur claude.ai | Vérifiez l'étape 2 (Autoriser les scripts utilisateur), puis rechargez la page avec F5. |
| Aucune jauge sur un autre site | C'est normal : elle n'apparaît que sur claude.ai par défaut. Utilisez **Afficher / masquer sur ce site** dans le menu Tampermonkey. |
| « ⚠ connecte-toi à claude.ai » | Vous êtes déconnecté : ouvrez claude.ai, connectez-vous, puis rechargez. |
| « ⚠ erreur : réseau » | L'accès à claude.ai n'a pas été autorisé : refaites l'étape 4 ou vérifiez votre connexion internet. |
| « ⚠ aucune limite trouvée » | Votre compte n'a pas de quota visible : c'est le cas d'un compte gratuit ou d'un compte uniquement API. |
| « ⚠ erreur : HTTP … », de façon durable | Claude a peut-être modifié son site, et le script doit être adapté. Prévenez la personne qui vous l'a transmis. |
| Pas de notification Windows | Paramètres Windows → Système → Notifications : autorisez Chrome ou Edge et désactivez le mode Ne pas déranger. |
| « Tampermonkey n'a pas accès à cette page » | C'est normal sur les pages internes du navigateur, comme les paramètres ou les extensions. |

En dernier recours, la page officielle [Paramètres → Utilisation](https://claude.ai/settings/usage) donne toujours les mêmes chiffres.

## Mettre à jour ou désinstaller

**Mettre à jour** quand une nouvelle version vous est transmise :

1. Cliquez sur l'icône Tampermonkey, puis sur **Tableau de bord**.
2. Cliquez sur le nom « Claude – jauge d'utilisation » pour l'ouvrir.
3. Sélectionnez tout (Ctrl+A), collez le nouveau code et enregistrez (Ctrl+S).
4. Rechargez vos onglets ouverts.

**Mettre en pause** : dans le tableau de bord, désactivez l'interrupteur vert du script.

**Désinstaller** : dans le tableau de bord, cliquez sur l'icône de corbeille du script. Pour tout retirer, supprimez ensuite l'extension Tampermonkey depuis la page des extensions.

**Modifier les seuils d'alerte** : en haut du script, la ligne `const SEUILS = [70, 90];` fixe les pourcentages d'alerte. La ligne `INTERVALLE_MIN = 5` fixe le délai entre deux actualisations, en minutes. Changez uniquement les chiffres, puis enregistrez.

## Annexe A : le script Tampermonkey (version 1.4)

Copiez tout le bloc, de `// ==UserScript==` jusqu'à `})();`. Il sert à la jauge du navigateur et alimente l'icône Windows.

```javascript
// ==UserScript==
// @name         Claude – jauge d'utilisation
// @namespace    madoe.fr
// @version      1.4
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
  const K_DATA = 'usage_data', K_ORG = 'org_id', K_NOTIF = 'notified';
  const K_PARTOUT = 'show_everywhere', K_SITES = 'sites_on', K_OFF = 'sites_off';
  const SITE = location.hostname;

  // Où afficher la jauge ? Par défaut : seulement sur claude.ai.
  // Les alertes Windows, elles, fonctionnent sur tous les sites.
  function visibleIci() {
    if (GM_getValue(K_OFF, []).includes(SITE)) return false;
    if (SITE === 'claude.ai' || SITE.endsWith('.claude.ai')) return true;
    return GM_getValue(K_PARTOUT, false) || GM_getValue(K_SITES, []).includes(SITE);
  }
  function basculer(cle, actif) {
    const l = GM_getValue(cle, []).filter(s => s !== SITE);
    if (actif) l.push(SITE);
    GM_setValue(cle, l);
  }

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
    if (!force && Date.now() - GM_getValue('en_cours', 0) < 30e3) return cache && render(cache);
    GM_setValue('en_cours', Date.now());
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
    if (!visibleIci()) { host && host.remove(); host = null; return; }
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
  GM_registerMenuCommand(`Afficher / masquer sur ${SITE}`, () => {
    const vis = visibleIci();
    basculer(K_OFF, vis);          // masquer ici
    basculer(K_SITES, !vis);       // ou afficher ici
    refresh();
  });
  GM_registerMenuCommand('Afficher partout / seulement sur claude.ai', () => {
    const p = !GM_getValue(K_PARTOUT, false);
    GM_setValue(K_PARTOUT, p);
    if (p) GM_setValue(K_OFF, []);  // repartir propre
    else GM_setValue(K_SITES, []);
    refresh();
  });
  GM_addValueChangeListener(K_DATA, (k, o, n, remote) => remote && n && render(n));

  refresh();
  setInterval(() => refresh(), 60e3);   // le cache partagé limite à 1 requête / 5 min
  document.addEventListener('visibilitychange', () => document.visibilityState === 'visible' && refresh());
})();
```

## Annexe B : jauge\_claude.pyw (icône Windows)

À enregistrer sous le nom `jauge_claude.pyw`, à côté de `installer.bat`. Inutile si vous avez l'archive `JaugeClaude.zip`.

```python
"""
Jauge Claude - icône dans la zone de notification Windows.

Reçoit les chiffres envoyés par le script Tampermonkey « Claude – jauge d'utilisation »
(v1.2 ou plus) et les affiche dans une icône près de l'horloge.

- L'icône montre le pourcentage le plus élevé (session ou semaine) et sa couleur :
  vert < 70 %, orange 70-89 %, rouge >= 90 %, gris si plus de nouvelles depuis 20 min.
- Survol : détail session / semaine et heures de remise à zéro.
- Clic droit : menu (ouvrir la page Utilisation, quitter).

Écoute uniquement sur 127.0.0.1 (rien n'est accessible depuis le réseau).
Dépendances : pip install pystray pillow
"""
import json
import threading
import time
import webbrowser
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import pystray
from PIL import Image, ImageDraw, ImageFont

PORT = 47321
OBSOLETE_APRES = 20 * 60          # secondes sans nouvelles avant de griser l'icône
URL_USAGE = "https://claude.ai/settings/usage"

VERT, ORANGE, ROUGE, GRIS = (46, 158, 91), (224, 161, 0), (214, 69, 69), (130, 130, 130)

etat = {"session": None, "semaine": None, "recu": 0.0}
verrou = threading.Lock()


# ---------------------------------------------------------------- dessin de l'icône
def _police(taille):
    for nom in ("segoeuib.ttf", "arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(nom, taille)
        except OSError:
            continue
    return ImageFont.load_default()


def couleur(pct):
    if pct is None:
        return GRIS
    return ROUGE if pct >= 90 else ORANGE if pct >= 70 else VERT


def dessiner(pct, obsolete):
    """Pastille ronde colorée avec le pourcentage au centre (64x64, réduit par Windows)."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fond = GRIS if obsolete else couleur(pct)
    d.ellipse((0, 0, 63, 63), fill=fond)
    texte = "?" if pct is None else ("!" if pct >= 100 else str(pct))
    police = _police(38 if len(texte) <= 1 else 32)
    l, t, r, b = d.textbbox((0, 0), texte, font=police)
    d.text(((64 - (r - l)) / 2 - l, (64 - (b - t)) / 2 - t), texte, font=police, fill="white")
    return img


# ---------------------------------------------------------------- textes
def quand(iso):
    if not iso:
        return "?"
    try:
        dt = datetime.fromisoformat(iso.replace("Z", "+00:00"))
    except ValueError:
        return "?"
    minutes = int((dt - datetime.now(timezone.utc)).total_seconds() // 60)
    if minutes < 60:
        return f"dans {max(minutes, 0)} min"
    if minutes < 24 * 60:
        return f"dans {minutes // 60} h {minutes % 60:02d}"
    jours = ["lun.", "mar.", "mer.", "jeu.", "ven.", "sam.", "dim."]
    local = dt.astimezone()
    return f"{jours[local.weekday()]} {local:%H:%M}"


def ligne(nom, v):
    return f"{nom} : {v['pct']} % (reset {quand(v.get('reset'))})" if v else f"{nom} : —"


def infobulle():
    with verrou:
        s, w, recu = etat["session"], etat["semaine"], etat["recu"]
    if not recu:
        return "Jauge Claude : en attente du navigateur"
    txt = f"{ligne('Session', s)}\n{ligne('Semaine', w)}"
    if time.time() - recu > OBSOLETE_APRES:
        txt += f"\nPas de nouvelles depuis {int((time.time() - recu) // 60)} min"
    return txt[:127]  # limite Windows


# ---------------------------------------------------------------- icône
def rafraichir(icone):
    with verrou:
        vals = [v["pct"] for v in (etat["session"], etat["semaine"]) if v]
        recu = etat["recu"]
    pct = max(vals) if vals else None
    obsolete = not recu or time.time() - recu > OBSOLETE_APRES
    icone.icon = dessiner(pct, obsolete)
    icone.title = infobulle()


def boucle(icone):
    icone.visible = True
    while True:  # met à jour les « dans x min » et l'état obsolète
        rafraichir(icone)
        time.sleep(30)


# ---------------------------------------------------------------- serveur local
def lire(v):
    if not isinstance(v, dict) or not isinstance(v.get("pct"), (int, float)):
        return None
    return {"pct": max(0, min(int(round(v["pct"])), 999)), "reset": str(v.get("reset") or "")[:40]}


class Recepteur(BaseHTTPRequestHandler):
    def do_POST(self):
        # En-tête personnalisé obligatoire : une page web ordinaire ne peut pas l'envoyer
        # vers 127.0.0.1 sans autorisation CORS, que ce serveur ne donne jamais.
        if self.path != "/usage" or self.headers.get("X-Jauge-Claude") != "1":
            self.send_error(403)
            return
        try:
            n = min(int(self.headers.get("Content-Length", 0)), 4096)
            data = json.loads(self.rfile.read(n))
        except (ValueError, json.JSONDecodeError):
            self.send_error(400)
            return
        with verrou:
            etat["session"], etat["semaine"], etat["recu"] = lire(data.get("session")), lire(data.get("semaine")), time.time()
        rafraichir(ICONE)
        self.send_response(204)
        self.end_headers()

    def log_message(self, *args):  # silencieux
        pass


def serveur():
    ThreadingHTTPServer(("127.0.0.1", PORT), Recepteur).serve_forever()


# ---------------------------------------------------------------- démarrage
def quitter(icone, _):
    icone.stop()


ICONE = pystray.Icon(
    "jauge_claude",
    dessiner(None, True),
    "Jauge Claude : en attente du navigateur",
    menu=pystray.Menu(
        pystray.MenuItem(lambda _: ligne("Session", etat["session"]), None, enabled=False),
        pystray.MenuItem(lambda _: ligne("Semaine", etat["semaine"]), None, enabled=False),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Ouvrir la page Utilisation", lambda *_: webbrowser.open(URL_USAGE), default=True),
        pystray.MenuItem("Quitter", quitter),
    ),
)

if __name__ == "__main__":
    threading.Thread(target=serveur, daemon=True).start()
    ICONE.run(setup=boucle)
```

## Annexe C : installer.bat

À enregistrer sous le nom `installer.bat`, dans le même dossier que `jauge_claude.pyw`.

```bat
@echo off
chcp 65001 >nul
setlocal
set "DOSSIER=%~dp0"
set "SCRIPT=%DOSSIER%jauge_claude.pyw"

echo === Jauge Claude : installation de l'icone Windows ===
echo.

where pythonw >nul 2>nul
if errorlevel 1 (
  echo Python n'est pas installe.
  echo Installez-le depuis le Microsoft Store ^(rechercher "Python 3.12"^) puis relancez ce fichier.
  pause
  exit /b 1
)

echo Installation des composants pystray et pillow...
python -m pip install --user --quiet --disable-pip-version-check pystray pillow
if errorlevel 1 (
  echo L'installation des composants a echoue.
  pause
  exit /b 1
)

echo Ajout au demarrage de Windows...
powershell -NoProfile -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Startup')+'\Jauge Claude.lnk');" ^
  "$s.TargetPath=(Get-Command pythonw).Source; $s.Arguments='\"%SCRIPT%\"'; $s.WorkingDirectory='%DOSSIER%'; $s.Save()"

echo Lancement...
start "" pythonw "%SCRIPT%"

echo.
echo Termine. Une pastille grise "?" apparait pres de l'horloge
echo ^(cliquez sur la fleche ^ si elle est masquee^).
echo Elle prend ses couleurs des que la jauge Tampermonkey v1.2 envoie les chiffres.
pause
```
