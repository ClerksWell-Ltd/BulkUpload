import { UMB_AUTH_CONTEXT as he } from "@umbraco-cms/backoffice/auth";
const me = {
  type: "section",
  alias: "BulkUpload.Section",
  name: "Bulk Upload",
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload"
  }
}, fe = {
  type: "dashboard",
  alias: "BulkUpload.Dashboard",
  name: "Bulk Upload Dashboard",
  element: () => Promise.resolve().then(() => Je),
  weight: -10,
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload-dashboard"
  },
  conditions: [
    {
      alias: "Umb.Condition.SectionAlias",
      match: "BulkUpload.Section"
    }
  ]
}, ge = [me, fe];
class ve {
  constructor() {
    this.config = null;
  }
  setConfig(e) {
    this.config = e;
  }
  getConfig() {
    return this.config;
  }
  async getAuthHeaders() {
    return this.config ? {
      Authorization: `Bearer ${await this.config.token()}`
    } : {};
  }
  getBaseUrl() {
    var e;
    return ((e = this.config) == null ? void 0 : e.baseUrl) || "";
  }
  getCredentials() {
    var e;
    return (e = this.config) == null ? void 0 : e.credentials;
  }
}
const y = new ve();
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const F = globalThis, L = F.ShadowRoot && (F.ShadyCSS === void 0 || F.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, V = Symbol(), Y = /* @__PURE__ */ new WeakMap();
let ne = class {
  constructor(e, t, i) {
    if (this._$cssResult$ = !0, i !== V) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = e, this.t = t;
  }
  get styleSheet() {
    let e = this.o;
    const t = this.t;
    if (L && e === void 0) {
      const i = t !== void 0 && t.length === 1;
      i && (e = Y.get(t)), e === void 0 && ((this.o = e = new CSSStyleSheet()).replaceSync(this.cssText), i && Y.set(t, e));
    }
    return e;
  }
  toString() {
    return this.cssText;
  }
};
const be = (r) => new ne(typeof r == "string" ? r : r + "", void 0, V), ye = (r, ...e) => {
  const t = r.length === 1 ? r[0] : e.reduce((i, s, o) => i + ((a) => {
    if (a._$cssResult$ === !0) return a.cssText;
    if (typeof a == "number") return a;
    throw Error("Value passed to 'css' function must be a 'css' function result: " + a + ". Use 'unsafeCSS' to pass non-literal values, but take care to ensure page security.");
  })(s) + r[o + 1], r[0]);
  return new ne(t, r, V);
}, xe = (r, e) => {
  if (L) r.adoptedStyleSheets = e.map((t) => t instanceof CSSStyleSheet ? t : t.styleSheet);
  else for (const t of e) {
    const i = document.createElement("style"), s = F.litNonce;
    s !== void 0 && i.setAttribute("nonce", s), i.textContent = t.cssText, r.appendChild(i);
  }
}, J = L ? (r) => r : (r) => r instanceof CSSStyleSheet ? ((e) => {
  let t = "";
  for (const i of e.cssRules) t += i.cssText;
  return be(t);
})(r) : r;
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const { is: $e, defineProperty: we, getOwnPropertyDescriptor: _e, getOwnPropertyNames: Ce, getOwnPropertySymbols: Ae, getPrototypeOf: Se } = Object, v = globalThis, G = v.trustedTypes, Ee = G ? G.emptyScript : "", N = v.reactiveElementPolyfillSupport, E = (r, e) => r, O = { toAttribute(r, e) {
  switch (e) {
    case Boolean:
      r = r ? Ee : null;
      break;
    case Object:
    case Array:
      r = r == null ? r : JSON.stringify(r);
  }
  return r;
}, fromAttribute(r, e) {
  let t = r;
  switch (e) {
    case Boolean:
      t = r !== null;
      break;
    case Number:
      t = r === null ? null : Number(r);
      break;
    case Object:
    case Array:
      try {
        t = JSON.parse(r);
      } catch {
        t = null;
      }
  }
  return t;
} }, Z = (r, e) => !$e(r, e), K = { attribute: !0, type: String, converter: O, reflect: !1, useDefault: !1, hasChanged: Z };
Symbol.metadata ?? (Symbol.metadata = Symbol("metadata")), v.litPropertyMetadata ?? (v.litPropertyMetadata = /* @__PURE__ */ new WeakMap());
let _ = class extends HTMLElement {
  static addInitializer(e) {
    this._$Ei(), (this.l ?? (this.l = [])).push(e);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(e, t = K) {
    if (t.state && (t.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(e) && ((t = Object.create(t)).wrapped = !0), this.elementProperties.set(e, t), !t.noAccessor) {
      const i = Symbol(), s = this.getPropertyDescriptor(e, i, t);
      s !== void 0 && we(this.prototype, e, s);
    }
  }
  static getPropertyDescriptor(e, t, i) {
    const { get: s, set: o } = _e(this.prototype, e) ?? { get() {
      return this[t];
    }, set(a) {
      this[t] = a;
    } };
    return { get: s, set(a) {
      const l = s == null ? void 0 : s.call(this);
      o == null || o.call(this, a), this.requestUpdate(e, l, i);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(e) {
    return this.elementProperties.get(e) ?? K;
  }
  static _$Ei() {
    if (this.hasOwnProperty(E("elementProperties"))) return;
    const e = Se(this);
    e.finalize(), e.l !== void 0 && (this.l = [...e.l]), this.elementProperties = new Map(e.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(E("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(E("properties"))) {
      const t = this.properties, i = [...Ce(t), ...Ae(t)];
      for (const s of i) this.createProperty(s, t[s]);
    }
    const e = this[Symbol.metadata];
    if (e !== null) {
      const t = litPropertyMetadata.get(e);
      if (t !== void 0) for (const [i, s] of t) this.elementProperties.set(i, s);
    }
    this._$Eh = /* @__PURE__ */ new Map();
    for (const [t, i] of this.elementProperties) {
      const s = this._$Eu(t, i);
      s !== void 0 && this._$Eh.set(s, t);
    }
    this.elementStyles = this.finalizeStyles(this.styles);
  }
  static finalizeStyles(e) {
    const t = [];
    if (Array.isArray(e)) {
      const i = new Set(e.flat(1 / 0).reverse());
      for (const s of i) t.unshift(J(s));
    } else e !== void 0 && t.push(J(e));
    return t;
  }
  static _$Eu(e, t) {
    const i = t.attribute;
    return i === !1 ? void 0 : typeof i == "string" ? i : typeof e == "string" ? e.toLowerCase() : void 0;
  }
  constructor() {
    super(), this._$Ep = void 0, this.isUpdatePending = !1, this.hasUpdated = !1, this._$Em = null, this._$Ev();
  }
  _$Ev() {
    var e;
    this._$ES = new Promise((t) => this.enableUpdating = t), this._$AL = /* @__PURE__ */ new Map(), this._$E_(), this.requestUpdate(), (e = this.constructor.l) == null || e.forEach((t) => t(this));
  }
  addController(e) {
    var t;
    (this._$EO ?? (this._$EO = /* @__PURE__ */ new Set())).add(e), this.renderRoot !== void 0 && this.isConnected && ((t = e.hostConnected) == null || t.call(e));
  }
  removeController(e) {
    var t;
    (t = this._$EO) == null || t.delete(e);
  }
  _$E_() {
    const e = /* @__PURE__ */ new Map(), t = this.constructor.elementProperties;
    for (const i of t.keys()) this.hasOwnProperty(i) && (e.set(i, this[i]), delete this[i]);
    e.size > 0 && (this._$Ep = e);
  }
  createRenderRoot() {
    const e = this.shadowRoot ?? this.attachShadow(this.constructor.shadowRootOptions);
    return xe(e, this.constructor.elementStyles), e;
  }
  connectedCallback() {
    var e;
    this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this.enableUpdating(!0), (e = this._$EO) == null || e.forEach((t) => {
      var i;
      return (i = t.hostConnected) == null ? void 0 : i.call(t);
    });
  }
  enableUpdating(e) {
  }
  disconnectedCallback() {
    var e;
    (e = this._$EO) == null || e.forEach((t) => {
      var i;
      return (i = t.hostDisconnected) == null ? void 0 : i.call(t);
    });
  }
  attributeChangedCallback(e, t, i) {
    this._$AK(e, i);
  }
  _$ET(e, t) {
    var o;
    const i = this.constructor.elementProperties.get(e), s = this.constructor._$Eu(e, i);
    if (s !== void 0 && i.reflect === !0) {
      const a = (((o = i.converter) == null ? void 0 : o.toAttribute) !== void 0 ? i.converter : O).toAttribute(t, i.type);
      this._$Em = e, a == null ? this.removeAttribute(s) : this.setAttribute(s, a), this._$Em = null;
    }
  }
  _$AK(e, t) {
    var o, a;
    const i = this.constructor, s = i._$Eh.get(e);
    if (s !== void 0 && this._$Em !== s) {
      const l = i.getPropertyOptions(s), n = typeof l.converter == "function" ? { fromAttribute: l.converter } : ((o = l.converter) == null ? void 0 : o.fromAttribute) !== void 0 ? l.converter : O;
      this._$Em = s;
      const d = n.fromAttribute(t, l.type);
      this[s] = d ?? ((a = this._$Ej) == null ? void 0 : a.get(s)) ?? d, this._$Em = null;
    }
  }
  requestUpdate(e, t, i, s = !1, o) {
    var a;
    if (e !== void 0) {
      const l = this.constructor;
      if (s === !1 && (o = this[e]), i ?? (i = l.getPropertyOptions(e)), !((i.hasChanged ?? Z)(o, t) || i.useDefault && i.reflect && o === ((a = this._$Ej) == null ? void 0 : a.get(e)) && !this.hasAttribute(l._$Eu(e, i)))) return;
      this.C(e, t, i);
    }
    this.isUpdatePending === !1 && (this._$ES = this._$EP());
  }
  C(e, t, { useDefault: i, reflect: s, wrapped: o }, a) {
    i && !(this._$Ej ?? (this._$Ej = /* @__PURE__ */ new Map())).has(e) && (this._$Ej.set(e, a ?? t ?? this[e]), o !== !0 || a !== void 0) || (this._$AL.has(e) || (this.hasUpdated || i || (t = void 0), this._$AL.set(e, t)), s === !0 && this._$Em !== e && (this._$Eq ?? (this._$Eq = /* @__PURE__ */ new Set())).add(e));
  }
  async _$EP() {
    this.isUpdatePending = !0;
    try {
      await this._$ES;
    } catch (t) {
      Promise.reject(t);
    }
    const e = this.scheduleUpdate();
    return e != null && await e, !this.isUpdatePending;
  }
  scheduleUpdate() {
    return this.performUpdate();
  }
  performUpdate() {
    var i;
    if (!this.isUpdatePending) return;
    if (!this.hasUpdated) {
      if (this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this._$Ep) {
        for (const [o, a] of this._$Ep) this[o] = a;
        this._$Ep = void 0;
      }
      const s = this.constructor.elementProperties;
      if (s.size > 0) for (const [o, a] of s) {
        const { wrapped: l } = a, n = this[o];
        l !== !0 || this._$AL.has(o) || n === void 0 || this.C(o, void 0, a, n);
      }
    }
    let e = !1;
    const t = this._$AL;
    try {
      e = this.shouldUpdate(t), e ? (this.willUpdate(t), (i = this._$EO) == null || i.forEach((s) => {
        var o;
        return (o = s.hostUpdate) == null ? void 0 : o.call(s);
      }), this.update(t)) : this._$EM();
    } catch (s) {
      throw e = !1, this._$EM(), s;
    }
    e && this._$AE(t);
  }
  willUpdate(e) {
  }
  _$AE(e) {
    var t;
    (t = this._$EO) == null || t.forEach((i) => {
      var s;
      return (s = i.hostUpdated) == null ? void 0 : s.call(i);
    }), this.hasUpdated || (this.hasUpdated = !0, this.firstUpdated(e)), this.updated(e);
  }
  _$EM() {
    this._$AL = /* @__PURE__ */ new Map(), this.isUpdatePending = !1;
  }
  get updateComplete() {
    return this.getUpdateComplete();
  }
  getUpdateComplete() {
    return this._$ES;
  }
  shouldUpdate(e) {
    return !0;
  }
  update(e) {
    this._$Eq && (this._$Eq = this._$Eq.forEach((t) => this._$ET(t, this[t]))), this._$EM();
  }
  updated(e) {
  }
  firstUpdated(e) {
  }
};
_.elementStyles = [], _.shadowRootOptions = { mode: "open" }, _[E("elementProperties")] = /* @__PURE__ */ new Map(), _[E("finalized")] = /* @__PURE__ */ new Map(), N == null || N({ ReactiveElement: _ }), (v.reactiveElementVersions ?? (v.reactiveElementVersions = [])).push("2.1.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const k = globalThis, X = (r) => r, q = k.trustedTypes, Q = q ? q.createPolicy("lit-html", { createHTML: (r) => r }) : void 0, le = "$lit$", g = `lit$${Math.random().toFixed(9).slice(2)}$`, de = "?" + g, ke = `<${de}>`, w = document, U = () => w.createComment(""), P = (r) => r === null || typeof r != "object" && typeof r != "function", W = Array.isArray, Me = (r) => W(r) || typeof (r == null ? void 0 : r[Symbol.iterator]) == "function", B = `[ 	
\f\r]`, S = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, ee = /-->/g, te = />/g, b = RegExp(`>|${B}(?:([^\\s"'>=/]+)(${B}*=${B}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), ie = /'/g, se = /"/g, ce = /^(?:script|style|textarea|title)$/i, Ue = (r) => (e, ...t) => ({ _$litType$: r, strings: e, values: t }), h = Ue(1), C = Symbol.for("lit-noChange"), c = Symbol.for("lit-nothing"), re = /* @__PURE__ */ new WeakMap(), x = w.createTreeWalker(w, 129);
function pe(r, e) {
  if (!W(r) || !r.hasOwnProperty("raw")) throw Error("invalid template strings array");
  return Q !== void 0 ? Q.createHTML(e) : e;
}
const Pe = (r, e) => {
  const t = r.length - 1, i = [];
  let s, o = e === 2 ? "<svg>" : e === 3 ? "<math>" : "", a = S;
  for (let l = 0; l < t; l++) {
    const n = r[l];
    let d, u, p = -1, m = 0;
    for (; m < n.length && (a.lastIndex = m, u = a.exec(n), u !== null); ) m = a.lastIndex, a === S ? u[1] === "!--" ? a = ee : u[1] !== void 0 ? a = te : u[2] !== void 0 ? (ce.test(u[2]) && (s = RegExp("</" + u[2], "g")), a = b) : u[3] !== void 0 && (a = b) : a === b ? u[0] === ">" ? (a = s ?? S, p = -1) : u[1] === void 0 ? p = -2 : (p = a.lastIndex - u[2].length, d = u[1], a = u[3] === void 0 ? b : u[3] === '"' ? se : ie) : a === se || a === ie ? a = b : a === ee || a === te ? a = S : (a = b, s = void 0);
    const f = a === b && r[l + 1].startsWith("/>") ? " " : "";
    o += a === S ? n + ke : p >= 0 ? (i.push(d), n.slice(0, p) + le + n.slice(p) + g + f) : n + g + (p === -2 ? l : f);
  }
  return [pe(r, o + (r[t] || "<?>") + (e === 2 ? "</svg>" : e === 3 ? "</math>" : "")), i];
};
class R {
  constructor({ strings: e, _$litType$: t }, i) {
    let s;
    this.parts = [];
    let o = 0, a = 0;
    const l = e.length - 1, n = this.parts, [d, u] = Pe(e, t);
    if (this.el = R.createElement(d, i), x.currentNode = this.el.content, t === 2 || t === 3) {
      const p = this.el.content.firstChild;
      p.replaceWith(...p.childNodes);
    }
    for (; (s = x.nextNode()) !== null && n.length < l; ) {
      if (s.nodeType === 1) {
        if (s.hasAttributes()) for (const p of s.getAttributeNames()) if (p.endsWith(le)) {
          const m = u[a++], f = s.getAttribute(p).split(g), z = /([.?@])?(.*)/.exec(m);
          n.push({ type: 1, index: o, name: z[2], strings: f, ctor: z[1] === "." ? Te : z[1] === "?" ? Ie : z[1] === "@" ? ze : H }), s.removeAttribute(p);
        } else p.startsWith(g) && (n.push({ type: 6, index: o }), s.removeAttribute(p));
        if (ce.test(s.tagName)) {
          const p = s.textContent.split(g), m = p.length - 1;
          if (m > 0) {
            s.textContent = q ? q.emptyScript : "";
            for (let f = 0; f < m; f++) s.append(p[f], U()), x.nextNode(), n.push({ type: 2, index: ++o });
            s.append(p[m], U());
          }
        }
      } else if (s.nodeType === 8) if (s.data === de) n.push({ type: 2, index: o });
      else {
        let p = -1;
        for (; (p = s.data.indexOf(g, p + 1)) !== -1; ) n.push({ type: 7, index: o }), p += g.length - 1;
      }
      o++;
    }
  }
  static createElement(e, t) {
    const i = w.createElement("template");
    return i.innerHTML = e, i;
  }
}
function A(r, e, t = r, i) {
  var a, l;
  if (e === C) return e;
  let s = i !== void 0 ? (a = t._$Co) == null ? void 0 : a[i] : t._$Cl;
  const o = P(e) ? void 0 : e._$litDirective$;
  return (s == null ? void 0 : s.constructor) !== o && ((l = s == null ? void 0 : s._$AO) == null || l.call(s, !1), o === void 0 ? s = void 0 : (s = new o(r), s._$AT(r, t, i)), i !== void 0 ? (t._$Co ?? (t._$Co = []))[i] = s : t._$Cl = s), s !== void 0 && (e = A(r, s._$AS(r, e.values), s, i)), e;
}
class Re {
  constructor(e, t) {
    this._$AV = [], this._$AN = void 0, this._$AD = e, this._$AM = t;
  }
  get parentNode() {
    return this._$AM.parentNode;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  u(e) {
    const { el: { content: t }, parts: i } = this._$AD, s = ((e == null ? void 0 : e.creationScope) ?? w).importNode(t, !0);
    x.currentNode = s;
    let o = x.nextNode(), a = 0, l = 0, n = i[0];
    for (; n !== void 0; ) {
      if (a === n.index) {
        let d;
        n.type === 2 ? d = new I(o, o.nextSibling, this, e) : n.type === 1 ? d = new n.ctor(o, n.name, n.strings, this, e) : n.type === 6 && (d = new Fe(o, this, e)), this._$AV.push(d), n = i[++l];
      }
      a !== (n == null ? void 0 : n.index) && (o = x.nextNode(), a++);
    }
    return x.currentNode = w, s;
  }
  p(e) {
    let t = 0;
    for (const i of this._$AV) i !== void 0 && (i.strings !== void 0 ? (i._$AI(e, i, t), t += i.strings.length - 2) : i._$AI(e[t])), t++;
  }
}
class I {
  get _$AU() {
    var e;
    return ((e = this._$AM) == null ? void 0 : e._$AU) ?? this._$Cv;
  }
  constructor(e, t, i, s) {
    this.type = 2, this._$AH = c, this._$AN = void 0, this._$AA = e, this._$AB = t, this._$AM = i, this.options = s, this._$Cv = (s == null ? void 0 : s.isConnected) ?? !0;
  }
  get parentNode() {
    let e = this._$AA.parentNode;
    const t = this._$AM;
    return t !== void 0 && (e == null ? void 0 : e.nodeType) === 11 && (e = t.parentNode), e;
  }
  get startNode() {
    return this._$AA;
  }
  get endNode() {
    return this._$AB;
  }
  _$AI(e, t = this) {
    e = A(this, e, t), P(e) ? e === c || e == null || e === "" ? (this._$AH !== c && this._$AR(), this._$AH = c) : e !== this._$AH && e !== C && this._(e) : e._$litType$ !== void 0 ? this.$(e) : e.nodeType !== void 0 ? this.T(e) : Me(e) ? this.k(e) : this._(e);
  }
  O(e) {
    return this._$AA.parentNode.insertBefore(e, this._$AB);
  }
  T(e) {
    this._$AH !== e && (this._$AR(), this._$AH = this.O(e));
  }
  _(e) {
    this._$AH !== c && P(this._$AH) ? this._$AA.nextSibling.data = e : this.T(w.createTextNode(e)), this._$AH = e;
  }
  $(e) {
    var o;
    const { values: t, _$litType$: i } = e, s = typeof i == "number" ? this._$AC(e) : (i.el === void 0 && (i.el = R.createElement(pe(i.h, i.h[0]), this.options)), i);
    if (((o = this._$AH) == null ? void 0 : o._$AD) === s) this._$AH.p(t);
    else {
      const a = new Re(s, this), l = a.u(this.options);
      a.p(t), this.T(l), this._$AH = a;
    }
  }
  _$AC(e) {
    let t = re.get(e.strings);
    return t === void 0 && re.set(e.strings, t = new R(e)), t;
  }
  k(e) {
    W(this._$AH) || (this._$AH = [], this._$AR());
    const t = this._$AH;
    let i, s = 0;
    for (const o of e) s === t.length ? t.push(i = new I(this.O(U()), this.O(U()), this, this.options)) : i = t[s], i._$AI(o), s++;
    s < t.length && (this._$AR(i && i._$AB.nextSibling, s), t.length = s);
  }
  _$AR(e = this._$AA.nextSibling, t) {
    var i;
    for ((i = this._$AP) == null ? void 0 : i.call(this, !1, !0, t); e !== this._$AB; ) {
      const s = X(e).nextSibling;
      X(e).remove(), e = s;
    }
  }
  setConnected(e) {
    var t;
    this._$AM === void 0 && (this._$Cv = e, (t = this._$AP) == null || t.call(this, e));
  }
}
class H {
  get tagName() {
    return this.element.tagName;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  constructor(e, t, i, s, o) {
    this.type = 1, this._$AH = c, this._$AN = void 0, this.element = e, this.name = t, this._$AM = s, this.options = o, i.length > 2 || i[0] !== "" || i[1] !== "" ? (this._$AH = Array(i.length - 1).fill(new String()), this.strings = i) : this._$AH = c;
  }
  _$AI(e, t = this, i, s) {
    const o = this.strings;
    let a = !1;
    if (o === void 0) e = A(this, e, t, 0), a = !P(e) || e !== this._$AH && e !== C, a && (this._$AH = e);
    else {
      const l = e;
      let n, d;
      for (e = o[0], n = 0; n < o.length - 1; n++) d = A(this, l[i + n], t, n), d === C && (d = this._$AH[n]), a || (a = !P(d) || d !== this._$AH[n]), d === c ? e = c : e !== c && (e += (d ?? "") + o[n + 1]), this._$AH[n] = d;
    }
    a && !s && this.j(e);
  }
  j(e) {
    e === c ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, e ?? "");
  }
}
class Te extends H {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(e) {
    this.element[this.name] = e === c ? void 0 : e;
  }
}
class Ie extends H {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(e) {
    this.element.toggleAttribute(this.name, !!e && e !== c);
  }
}
class ze extends H {
  constructor(e, t, i, s, o) {
    super(e, t, i, s, o), this.type = 5;
  }
  _$AI(e, t = this) {
    if ((e = A(this, e, t, 0) ?? c) === C) return;
    const i = this._$AH, s = e === c && i !== c || e.capture !== i.capture || e.once !== i.once || e.passive !== i.passive, o = e !== c && (i === c || s);
    s && this.element.removeEventListener(this.name, this, i), o && this.element.addEventListener(this.name, this, e), this._$AH = e;
  }
  handleEvent(e) {
    var t;
    typeof this._$AH == "function" ? this._$AH.call(((t = this.options) == null ? void 0 : t.host) ?? this.element, e) : this._$AH.handleEvent(e);
  }
}
class Fe {
  constructor(e, t, i) {
    this.element = e, this.type = 6, this._$AN = void 0, this._$AM = t, this.options = i;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(e) {
    A(this, e);
  }
}
const D = k.litHtmlPolyfillSupport;
D == null || D(R, I), (k.litHtmlVersions ?? (k.litHtmlVersions = [])).push("3.3.2");
const Oe = (r, e, t) => {
  const i = (t == null ? void 0 : t.renderBefore) ?? e;
  let s = i._$litPart$;
  if (s === void 0) {
    const o = (t == null ? void 0 : t.renderBefore) ?? null;
    i._$litPart$ = s = new I(e.insertBefore(U(), o), o, void 0, t ?? {});
  }
  return s._$AI(r), s;
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const $ = globalThis;
class M extends _ {
  constructor() {
    super(...arguments), this.renderOptions = { host: this }, this._$Do = void 0;
  }
  createRenderRoot() {
    var t;
    const e = super.createRenderRoot();
    return (t = this.renderOptions).renderBefore ?? (t.renderBefore = e.firstChild), e;
  }
  update(e) {
    const t = this.render();
    this.hasUpdated || (this.renderOptions.isConnected = this.isConnected), super.update(e), this._$Do = Oe(t, this.renderRoot, this.renderOptions);
  }
  connectedCallback() {
    var e;
    super.connectedCallback(), (e = this._$Do) == null || e.setConnected(!0);
  }
  disconnectedCallback() {
    var e;
    super.disconnectedCallback(), (e = this._$Do) == null || e.setConnected(!1);
  }
  render() {
    return C;
  }
}
var ae;
M._$litElement$ = !0, M.finalized = !0, (ae = $.litElementHydrateSupport) == null || ae.call($, { LitElement: M });
const j = $.litElementPolyfillSupport;
j == null || j({ LitElement: M });
($.litElementVersions ?? ($.litElementVersions = [])).push("4.2.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const qe = (r) => (e, t) => {
  t !== void 0 ? t.addInitializer(() => {
    customElements.define(r, e);
  }) : customElements.define(r, e);
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const He = { attribute: !0, type: String, converter: O, reflect: !1, hasChanged: Z }, Ne = (r = He, e, t) => {
  const { kind: i, metadata: s } = t;
  let o = globalThis.litPropertyMetadata.get(s);
  if (o === void 0 && globalThis.litPropertyMetadata.set(s, o = /* @__PURE__ */ new Map()), i === "setter" && ((r = Object.create(r)).wrapped = !0), o.set(t.name, r), i === "accessor") {
    const { name: a } = t;
    return { set(l) {
      const n = e.get.call(this);
      e.set.call(this, l), this.requestUpdate(a, n, r, !0, l);
    }, init(l) {
      return l !== void 0 && this.C(a, void 0, r, l), l;
    } };
  }
  if (i === "setter") {
    const { name: a } = t;
    return function(l) {
      const n = this[a];
      e.call(this, l), this.requestUpdate(a, n, r, !0, l);
    };
  }
  throw Error("Unsupported decorator location: " + i);
};
function Be(r) {
  return (e, t) => typeof t == "object" ? Ne(r, e, t) : ((i, s, o) => {
    const a = s.hasOwnProperty(o);
    return s.constructor.createProperty(o, i), a ? Object.getOwnPropertyDescriptor(s, o) : void 0;
  })(r, e, t);
}
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
function De(r) {
  return Be({ ...r, state: !0, attribute: !1 });
}
class je {
  /**
   * Imports content from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importContent(e) {
    if (!e)
      throw new Error("File is required for content import");
    try {
      return await this.post(
        "/api/v1/content/importall",
        e
      );
    } catch (t) {
      throw new Error("Content import failed: " + t.message);
    }
  }
  /**
   * Imports media from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importMedia(e) {
    if (!e)
      throw new Error("File is required for media import");
    try {
      return await this.post(
        "/api/v1/media/importmedia",
        e
      );
    } catch (t) {
      throw new Error("Media import failed: " + t.message);
    }
  }
  /**
   * Exports content import results to CSV or ZIP
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportContentResults(e) {
    if (!e || !Array.isArray(e))
      throw new Error("Results array is required for export");
    try {
      return await this.postForBlob(
        "/api/v1/content/exportresults",
        e
      );
    } catch (t) {
      throw new Error("Export failed: " + t.message);
    }
  }
  /**
   * Exports media import results to CSV
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportMediaResults(e) {
    if (!e || !Array.isArray(e))
      throw new Error("Results array is required for export");
    try {
      return await this.postForBlob(
        "/api/v1/media/exportresults",
        e
      );
    } catch (t) {
      throw new Error("Export failed: " + t.message);
    }
  }
  /**
   * Validates a file before import
   * @param file - The file to validate
   * @param options - Validation options
   * @returns Validation result with valid flag and errors array
   */
  validateFile(e, t = {}) {
    var d;
    const i = [];
    if (!e)
      return i.push("No file selected"), { valid: !1, errors: i };
    const s = t.acceptedTypes || [".csv", ".zip"], o = ((d = e.name.split(".").pop()) == null ? void 0 : d.toLowerCase()) || "";
    s.some((u) => (u.startsWith(".") ? u.slice(1) : u).toLowerCase() === o) || i.push(`File type .${o} is not accepted. Accepted types: ${s.join(", ")}`);
    const l = t.maxSizeInMB || 100, n = l * 1024 * 1024;
    return e.size > n && i.push(`File size (${Math.round(e.size / 1024 / 1024)}MB) exceeds maximum (${l}MB)`), {
      valid: i.length === 0,
      errors: i
    };
  }
  /**
   * Internal: POST request for JSON data
   */
  async post(e, t) {
    let i;
    const s = {}, o = await y.getAuthHeaders();
    if (Object.assign(s, o), t instanceof File) {
      const p = new FormData();
      p.append("file", t), i = p;
    } else
      i = JSON.stringify(t), s["Content-Type"] = "application/json";
    const a = y.getBaseUrl(), l = a ? `${a}${e}` : e, n = y.getCredentials(), d = await fetch(l, {
      method: "POST",
      body: i,
      headers: s,
      credentials: n
    });
    if (!d.ok)
      throw new Error(`HTTP ${d.status}: ${d.statusText}`);
    return {
      data: await d.json(),
      status: d.status,
      headers: d.headers
    };
  }
  /**
   * Internal: POST request for blob/file downloads
   */
  async postForBlob(e, t) {
    const s = {
      "Content-Type": "application/json",
      ...await y.getAuthHeaders()
    }, o = y.getBaseUrl(), a = o ? `${o}${e}` : e, l = y.getCredentials(), n = await fetch(a, {
      method: "POST",
      body: JSON.stringify(t),
      headers: s,
      credentials: l
    });
    if (!n.ok)
      throw new Error(`HTTP ${n.status}: ${n.statusText}`);
    return n;
  }
}
class Le {
  constructor(e, t, i) {
    if (!e)
      throw new Error("API client is required");
    if (!t)
      throw new Error("Notification handler is required");
    this.apiClient = e, this.notify = t, this.onStateChange = i || null, this.state = this.createInitialState();
  }
  /**
   * Creates initial state structure
   */
  createInitialState() {
    return {
      activeTab: "content",
      content: {
        loading: !1,
        file: null,
        fileElement: null,
        results: null
      },
      media: {
        loading: !1,
        file: null,
        fileElement: null,
        results: null
      }
    };
  }
  /**
   * Emits state change event
   */
  emitStateChange() {
    this.onStateChange && this.onStateChange({ ...this.state });
  }
  /**
   * Sets the active tab
   */
  setActiveTab(e) {
    if (e !== "content" && e !== "media")
      throw new Error('Invalid tab name. Must be "content" or "media"');
    this.state.activeTab = e, this.emitStateChange();
  }
  /**
   * Sets content file and file element
   */
  setContentFile(e, t) {
    this.state.content.file = e, this.state.content.fileElement = t || null, this.emitStateChange();
  }
  /**
   * Sets media file and file element
   */
  setMediaFile(e, t) {
    this.state.media.file = e, this.state.media.fileElement = t || null, this.emitStateChange();
  }
  /**
   * Clears content file
   */
  clearContentFile() {
    this.state.content.file = null, this.state.content.fileElement && (this.state.content.fileElement.value = ""), this.emitStateChange();
  }
  /**
   * Clears media file
   */
  clearMediaFile() {
    this.state.media.file = null, this.state.media.fileElement && (this.state.media.fileElement.value = ""), this.emitStateChange();
  }
  /**
   * Clears content results
   */
  clearContentResults() {
    this.state.content.results = null, this.emitStateChange();
  }
  /**
   * Clears media results
   */
  clearMediaResults() {
    this.state.media.results = null, this.emitStateChange();
  }
  /**
   * Imports content from selected file
   */
  async importContent() {
    const e = this.state.content.file;
    if (!e)
      return this.notify({
        type: "warning",
        headline: "No File Selected",
        message: "Please select a CSV or ZIP file to import."
      }), null;
    const t = this.apiClient.validateFile(e, {
      acceptedTypes: [".csv", ".zip"],
      maxSizeInMB: 100
    });
    if (!t.valid)
      return this.notify({
        type: "error",
        headline: "Invalid File",
        message: t.errors.join(", ")
      }), null;
    this.state.content.loading = !0, this.state.content.results = null, this.emitStateChange();
    try {
      const i = await this.apiClient.importContent(e);
      this.clearContentFile(), this.state.content.results = i.data;
      const s = {
        total: i.data.totalCount || 0,
        success: i.data.successCount || 0,
        failed: i.data.failureCount || 0
      };
      let o;
      return s.total === 0 ? o = "No content items to import." : s.failed === 0 ? o = `All ${s.total} content items imported successfully.` : s.success === 0 ? o = `All ${s.total} content items failed to import.` : o = `${s.success} of ${s.total} content items imported successfully. ${s.failed} failed.`, this.notify({
        type: s.failed > 0 ? "warning" : "success",
        headline: "Content Import Complete",
        message: o
      }), i.data;
    } catch (i) {
      throw this.notify({
        type: "error",
        headline: "Import Failed",
        message: i.message || "An error occurred during content import."
      }), i;
    } finally {
      this.state.content.loading = !1, this.emitStateChange();
    }
  }
  /**
   * Imports media from selected file
   */
  async importMedia() {
    const e = this.state.media.file;
    if (!e)
      return this.notify({
        type: "warning",
        headline: "No File Selected",
        message: "Please select a CSV or ZIP file to import."
      }), null;
    const t = this.apiClient.validateFile(e, {
      acceptedTypes: [".csv", ".zip"],
      maxSizeInMB: 100
    });
    if (!t.valid)
      return this.notify({
        type: "error",
        headline: "Invalid File",
        message: t.errors.join(", ")
      }), null;
    this.state.media.loading = !0, this.state.media.results = null, this.emitStateChange();
    try {
      const i = await this.apiClient.importMedia(e);
      this.clearMediaFile(), this.state.media.results = i.data;
      const s = {
        total: i.data.totalCount || 0,
        success: i.data.successCount || 0,
        failed: i.data.failureCount || 0
      };
      let o;
      return s.total === 0 ? o = "No media items to import." : s.failed === 0 ? o = `All ${s.total} media items imported successfully.` : s.success === 0 ? o = `All ${s.total} media items failed to import.` : o = `${s.success} of ${s.total} media items imported successfully. ${s.failed} failed.`, this.notify({
        type: s.failed > 0 ? "warning" : "success",
        headline: "Media Import Complete",
        message: o
      }), i.data;
    } catch (i) {
      throw this.notify({
        type: "error",
        headline: "Import Failed",
        message: i.message || "An error occurred during media import."
      }), i;
    } finally {
      this.state.media.loading = !1, this.emitStateChange();
    }
  }
  /**
   * Exports content import results to CSV
   */
  async exportContentResults() {
    const e = this.state.content.results;
    if (!e || !e.results)
      return this.notify({
        type: "warning",
        headline: "No Results",
        message: "No results available to export."
      }), null;
    try {
      const t = await this.apiClient.exportContentResults(e.results);
      return this.notify({
        type: "success",
        headline: "Export Successful",
        message: "Results exported successfully."
      }), t;
    } catch (t) {
      throw this.notify({
        type: "error",
        headline: "Export Failed",
        message: "Failed to export results."
      }), t;
    }
  }
  /**
   * Exports media import results to CSV
   */
  async exportMediaResults() {
    const e = this.state.media.results;
    if (!e || !e.results)
      return this.notify({
        type: "warning",
        headline: "No Results",
        message: "No results available to export."
      }), null;
    try {
      const t = await this.apiClient.exportMediaResults(e.results);
      return this.notify({
        type: "success",
        headline: "Export Successful",
        message: "Results exported successfully."
      }), t;
    } catch (t) {
      throw this.notify({
        type: "error",
        headline: "Export Failed",
        message: "Failed to export results."
      }), t;
    }
  }
  /**
   * Gets current state (for debugging or serialization)
   */
  getState() {
    return { ...this.state };
  }
  /**
   * Resets service to initial state
   */
  reset() {
    this.state = this.createInitialState(), this.emitStateChange();
  }
}
function Ve(r) {
  if (!r || r === 0) return "0 Bytes";
  const e = 1024, t = ["Bytes", "KB", "MB", "GB", "TB"], i = Math.floor(Math.log(r) / Math.log(e));
  return `${Math.round(r / Math.pow(e, i) * 100) / 100} ${t[i]}`;
}
function Ze(r, e) {
  const t = document.createElement("a");
  t.href = window.URL.createObjectURL(r), t.download = e, t.click(), setTimeout(() => {
    window.URL.revokeObjectURL(t.href);
  }, 100);
}
async function oe(r, e) {
  if (!r || !r.ok) {
    console.error("Invalid response for file download");
    return;
  }
  const t = await r.blob();
  let i = e;
  const s = r.headers.get("content-type"), o = r.headers.get("content-disposition");
  if (o) {
    const a = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(o);
    a != null && a[1] && (i = a[1].replace(/['"]/g, ""));
  }
  (!i || i === e) && s && s.indexOf("application/zip") !== -1 && (i = e.replace(/\.csv$/, ".zip")), Ze(t, i);
}
var We = Object.defineProperty, Ye = Object.getOwnPropertyDescriptor, ue = (r, e, t, i) => {
  for (var s = i > 1 ? void 0 : i ? Ye(e, t) : e, o = r.length - 1, a; o >= 0; o--)
    (a = r[o]) && (s = (i ? a(e, t, s) : a(s)) || s);
  return i && s && We(e, t, s), s;
};
let T = class extends M {
  constructor() {
    super();
    const r = new je();
    this.service = new Le(
      r,
      this.handleNotification.bind(this),
      this.handleStateChange.bind(this)
    ), this.dashboardState = this.service.getState();
  }
  handleNotification(r) {
    this.dispatchEvent(new CustomEvent("notification", {
      detail: { notification: r },
      bubbles: !0,
      composed: !0
    }));
  }
  handleStateChange(r) {
    this.dashboardState = { ...r };
  }
  handleContentFileChange(r) {
    var i;
    const e = r.target, t = (i = e.files) == null ? void 0 : i[0];
    t && this.service.setContentFile(t, e);
  }
  handleMediaFileChange(r) {
    var i;
    const e = r.target, t = (i = e.files) == null ? void 0 : i[0];
    t && this.service.setMediaFile(t, e);
  }
  triggerFileInput(r) {
    var i;
    const e = r === "content" ? "content-file-input" : "media-file-input", t = (i = this.shadowRoot) == null ? void 0 : i.getElementById(e);
    t && t.click();
  }
  async handleContentImport() {
    await this.service.importContent(), setTimeout(() => {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }, 100);
  }
  async handleMediaImport() {
    await this.service.importMedia(), setTimeout(() => {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }, 100);
  }
  async handleContentExport() {
    const r = await this.service.exportContentResults();
    r && await oe(r, "content-results.csv");
  }
  async handleMediaExport() {
    const r = await this.service.exportMediaResults();
    r && await oe(r, "media-results.csv");
  }
  render() {
    const { activeTab: r, content: e, media: t } = this.dashboardState;
    return h`
      <div class="bulk-upload-dashboard">
        <!-- Page Header -->
        <div class="page-header">
          <div>
            <h1>Bulk Upload</h1>
            <p>Import content and media into your Umbraco site in bulk via CSV or ZIP files.</p>
            <div class="header-badge">
              <span class="dot"></span>
              Ready to import
            </div>
          </div>
        </div>

        <!-- Tab Navigation -->
        <uui-tab-group style="margin-bottom: 28px;">
          <uui-tab
            label="Content Import"
            ?active=${r === "content"}
            @click=${() => this.service.setActiveTab("content")}>
            Content Import
          </uui-tab>
          <uui-tab
            label="Media Import"
            ?active=${r === "media"}
            @click=${() => this.service.setActiveTab("media")}>
            Media Import
          </uui-tab>
        </uui-tab-group>

        <!-- Content Import Panel -->
        ${r === "content" ? h`
          <div class="tab-panel">
            ${e.results && !e.loading ? this.renderResults("content", e.results) : c}
            ${this.renderUploadCard("content", e)}
            ${e.loading ? this.renderLoadingState("content") : c}
            ${e.loading ? c : this.renderContentRequirements()}
          </div>
        ` : c}

        <!-- Media Import Panel -->
        ${r === "media" ? h`
          <div class="tab-panel">
            ${t.results && !t.loading ? this.renderResults("media", t.results) : c}
            ${this.renderUploadCard("media", t)}
            ${t.loading ? this.renderLoadingState("media") : c}
            ${t.loading ? c : this.renderMediaRequirements()}
          </div>
        ` : c}

        <!-- Footer -->
        ${this.renderFooter()}
      </div>
    `;
  }
  renderUploadCard(r, e) {
    const t = r === "content", i = t ? "content-file-input" : "media-file-input";
    return h`
      <div class="upload-card">
        <div class="card-header">
          <div class="icon-circle green">▲</div>
          <div>
            <h2>${"Upload File"}</h2>
            <span class="subtitle">${t ? "Drag & drop or browse for your import file" : "Drag & drop or browse for your media import file"}</span>
          </div>
        </div>
        <div class="card-body">
          <!-- Drop Zone -->
          ${e.file ? c : h`
            <div class="drop-zone" @click=${() => this.triggerFileInput(r)}>
              <div class="upload-icon">▲</div>
              <h3>Drag files here or <em>browse</em></h3>
              <p>Select a ${t ? "file to start your content import" : "ZIP or CSV file to start your media import"}</p>
              <div class="file-types">
                <span class="file-chip">.csv</span>
                <span class="file-chip">.zip</span>
              </div>
            </div>
          `}

          <!-- File Preview -->
          ${e.file && !e.loading ? h`
            <div class="file-preview">
              <div class="file-icon">📄</div>
              <div class="file-details">
                <div class="file-name">${e.file.name}</div>
                <div class="file-size">${Ve(e.file.size)}</div>
              </div>
              <uui-button
                label="Clear"
                look="outline"
                @click=${() => t ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            </div>
          ` : c}

          <!-- Hidden File Input -->
          <input
            type="file"
            id=${i}
            accept=".csv,.zip"
            ?disabled=${e.loading}
            @change=${t ? this.handleContentFileChange : this.handleMediaFileChange}
          />

          <!-- Import Button -->
          <div class="btn-container">
            <uui-button
              label=${t ? "Import Content" : "Import Media"}
              look="primary"
              color="positive"
              ?disabled=${!e.file || e.loading}
              @click=${t ? this.handleContentImport : this.handleMediaImport}
              style="--uui-button-padding: 12px 28px;">
              ${e.loading ? "Processing..." : `▲ Import ${t ? "Content" : "Media"}`}
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }
  renderLoadingState(r) {
    return h`
      <div class="loading-state">
        <uui-loader-bar></uui-loader-bar>
        <p>Importing ${r}, please wait...</p>
      </div>
    `;
  }
  renderContentRequirements() {
    return h`
      <div class="requirements-card">
        <div class="card-header">
          <div class="icon-circle blue">ℹ</div>
          <div>
            <h2>Requirements & Help</h2>
            <span class="subtitle">Everything you need to format your import file correctly</span>
          </div>
        </div>
        <div class="card-body">
          <!-- Required CSV Columns -->
          <div class="req-section">
            <div class="req-label">Required CSV Columns</div>
            <div class="req-grid">
              <div class="req-item">
                <code>parent</code>
                <div class="desc">Parent ID, GUID, or content path</div>
                <div class="examples">
                  <span>1050</span>
                  <span>71332aa7-…</span>
                  <span>/news/2024/</span>
                </div>
              </div>
              <div class="req-item">
                <code>docTypeAlias</code>
                <div class="desc">Content type alias</div>
                <div class="examples">
                  <span>articlePage</span>
                </div>
              </div>
              <div class="req-item">
                <code>name</code>
                <div class="desc">Content item name</div>
                <div class="examples">
                  <span>My Article</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Media Files -->
          <div class="req-section">
            <div class="req-label">Media Files</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">▲</div>
                Upload a <strong>ZIP file</strong> to include media with your content
              </div>
              <div class="media-tip">
                <div class="tip-icon">🖼</div>
                Reference media using resolvers like <code>heroImage|zipFileToMedia</code>
              </div>
              <div class="media-tip">
                <div class="tip-icon">👥</div>
                Supports multi-CSV imports with automatic media deduplication
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                Add extra CSV columns for any property on your doc type
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }
  renderMediaRequirements() {
    return h`
      <div class="requirements-card">
        <div class="card-header">
          <div class="icon-circle blue">ℹ</div>
          <div>
            <h2>Requirements & Help</h2>
            <span class="subtitle">Everything you need to format your media import file correctly</span>
          </div>
        </div>
        <div class="card-body">
          <!-- Upload Options -->
          <div class="req-section">
            <div class="req-label">Upload Options</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">📦</div>
                <strong>ZIP file:</strong> Contains CSV and media files referenced in it
              </div>
              <div class="media-tip">
                <div class="tip-icon">📄</div>
                <strong>CSV only:</strong> For URL or file path imports
              </div>
            </div>
          </div>

          <!-- Required Columns -->
          <div class="req-section">
            <div class="req-label">Required CSV Columns (depends on import type)</div>
            <div class="req-grid">
              <div class="req-item">
                <code>fileName</code>
                <div class="desc">For ZIP uploads: path to file within ZIP</div>
                <div class="examples">
                  <span>image.jpg</span>
                  <span>photos/pic.png</span>
                </div>
              </div>
              <div class="req-item">
                <code>mediaSource|urlToStream</code>
                <div class="desc">For URL imports</div>
                <div class="examples">
                  <span>https://...</span>
                </div>
              </div>
              <div class="req-item">
                <code>mediaSource|pathToStream</code>
                <div class="desc">For file path imports</div>
                <div class="examples">
                  <span>C:\\Images\\...</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Optional Features -->
          <div class="req-section">
            <div class="req-label">Optional Features</div>
            <div class="media-tips">
              <div class="media-tip">
                <div class="tip-icon">📁</div>
                <code>parent</code> column for folder ID/GUID/path (auto-creates folders)
              </div>
              <div class="media-tip">
                <div class="tip-icon">🏷</div>
                <code>name</code> and <code>mediaTypeAlias</code> (auto-detected if omitted)
              </div>
              <div class="media-tip">
                <div class="tip-icon">👥</div>
                Multi-CSV imports with automatic media deduplication
              </div>
              <div class="media-tip">
                <div class="tip-icon">✨</div>
                Custom properties like <code>altText</code>, <code>caption</code>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }
  renderResults(r, e) {
    const t = {
      total: e.totalCount || 0,
      success: e.successCount || 0,
      failed: e.failureCount || 0
    };
    return h`
      <div class="upload-card">
        <div class="card-header">
          <div class="icon-circle green">✓</div>
          <div>
            <h2>${"Import Results"}</h2>
            <span class="subtitle">${r === "content" ? "Summary of your content import" : "Summary of your media import"}</span>
          </div>
        </div>
        <div class="card-body">
          <div class="results-summary">
            <div class="badge badge-total">
              <strong>Total:</strong> ${t.total}
            </div>
            <div class="badge badge-success">
              <strong>✓ Success:</strong> ${t.success}
            </div>
            ${t.failed > 0 ? h`
              <div class="badge badge-failed">
                <strong>✗ Failed:</strong> ${t.failed}
              </div>
            ` : c}
          </div>

          <div style="display: flex; gap: 10px;">
            <uui-button
              label="Download Results CSV"
              look="outline"
              @click=${r === "content" ? this.handleContentExport : this.handleMediaExport}
              style="--uui-button-padding: 10px 20px;">
              ⬇ Download Results CSV
            </uui-button>
            <uui-button
              label="Clear Results"
              look="outline"
              @click=${() => r === "content" ? this.service.clearContentResults() : this.service.clearMediaResults()}
              style="--uui-button-padding: 10px 20px;">
              Clear Results
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }
  renderFooter() {
    return h`
      <footer class="plugin-footer">
        <div class="divider"></div>
        <a href="https://www.clerkswell.com" target="_blank" rel="noopener noreferrer" class="brand-link">
          Made for the Umbraco Community with
          <span class="heart">❤️</span>
          from
          <img src="/App_Plugins/BulkUpload/images/cw-logo-primary-blue.png" alt="ClerksWell" />
        </a>
      </footer>
    `;
  }
};
T.styles = ye`
    :host {
      display: block;
    }

    /* Import shared variables */
    .bulk-upload-dashboard {
      --umb-blue: #1b264f;
      --umb-blue-light: #2c3e6b;
      --umb-blue-hover: #243561;
      --umb-surface: #f6f7fb;
      --umb-white: #ffffff;
      --umb-border: #e0e3eb;
      --umb-text: #303033;
      --umb-text-muted: #68697a;
      --umb-accent: #2bc37b;
      --umb-accent-soft: #e6f9f0;
      --umb-accent-hover: #25a86a;
      --umb-danger: #d42054;
      --umb-warning: #f5c142;
      --umb-code-bg: #f0f1f5;
      --umb-shadow-sm: 0 1px 3px rgba(27,38,79,0.06);
      --umb-shadow-md: 0 4px 12px rgba(27,38,79,0.08);
      --umb-shadow-lg: 0 8px 32px rgba(27,38,79,0.12);
      --umb-radius: 8px;
      --umb-radius-lg: 12px;

      max-width: 960px;
      margin: 0 auto;
      padding: 20px;
      animation: fadeUp 0.5s ease both;
    }

    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(12px); }
      to { opacity: 1; transform: translateY(0); }
    }

    /* Header */
    .page-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      margin-bottom: 24px;
    }

    .page-header h1 {
      font-size: 28px;
      font-weight: 900;
      color: var(--umb-blue);
      letter-spacing: -0.5px;
      margin: 0;
    }

    .page-header p {
      color: var(--umb-text-muted);
      font-size: 14px;
      margin-top: 6px;
      line-height: 1.5;
    }

    .header-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
      font-size: 12px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 20px;
      margin-top: 10px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .header-badge .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: var(--umb-accent);
    }

    /* Tab panel */
    .tab-panel {
      animation: fadeIn 0.3s ease-in;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    /* Cards */
    .upload-card,
    .requirements-card {
      background: var(--umb-white);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius-lg);
      box-shadow: var(--umb-shadow-sm);
      overflow: hidden;
      margin-bottom: 24px;
      transition: box-shadow .25s;
    }

    .upload-card:hover,
    .requirements-card:hover {
      box-shadow: var(--umb-shadow-md);
    }

    .card-header {
      padding: 20px 28px;
      display: flex;
      align-items: center;
      gap: 12px;
      border-bottom: 1px solid var(--umb-border);
      background: linear-gradient(180deg, #fafbfe 0%, var(--umb-white) 100%);
    }

    .card-header .icon-circle {
      width: 36px;
      height: 36px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      font-size: 18px;
    }

    .card-header .icon-circle.blue {
      background: rgba(27,38,79,0.08);
      color: var(--umb-blue);
    }

    .card-header .icon-circle.green {
      background: var(--umb-accent-soft);
      color: var(--umb-accent-hover);
    }

    .card-header h2 {
      font-size: 15px;
      font-weight: 700;
      color: var(--umb-blue);
      margin: 0;
    }

    .card-header .subtitle {
      font-size: 12px;
      color: var(--umb-text-muted);
      display: block;
      margin-top: 2px;
    }

    .card-body {
      padding: 28px;
    }

    /* Drop zone */
    .drop-zone {
      border: 2px dashed var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 48px 32px;
      text-align: center;
      transition: border-color .25s, background .25s, transform .15s;
      cursor: pointer;
    }

    .drop-zone:hover {
      border-color: var(--umb-accent);
      background: var(--umb-accent-soft);
      transform: scale(1.005);
    }

    .drop-zone .upload-icon {
      width: 56px;
      height: 56px;
      border-radius: 16px;
      background: linear-gradient(135deg, var(--umb-accent-soft) 0%, #d4f3e4 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 16px;
      color: var(--umb-accent);
      transition: transform .3s;
      font-size: 24px;
    }

    .drop-zone:hover .upload-icon {
      transform: translateY(-4px);
    }

    .drop-zone h3 {
      font-size: 15px;
      font-weight: 700;
      color: var(--umb-text);
      margin-bottom: 6px;
    }

    .drop-zone h3 em {
      font-style: normal;
      color: var(--umb-accent);
      text-decoration: underline;
      text-underline-offset: 2px;
    }

    .drop-zone p {
      font-size: 13px;
      color: var(--umb-text-muted);
      margin: 0;
    }

    .drop-zone .file-types {
      display: flex;
      gap: 8px;
      justify-content: center;
      margin-top: 16px;
    }

    .file-chip {
      background: var(--umb-code-bg);
      color: var(--umb-text-muted);
      font-size: 11px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 6px;
      letter-spacing: 0.3px;
      text-transform: uppercase;
    }

    /* File preview */
    .file-preview {
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 16px;
      margin: 16px 0;
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .file-preview .file-icon {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      background: var(--umb-accent-soft);
      color: var(--umb-accent);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      flex-shrink: 0;
    }

    .file-preview .file-details {
      flex: 1;
    }

    .file-preview .file-name {
      font-weight: 700;
      color: var(--umb-text);
      font-size: 14px;
    }

    .file-preview .file-size {
      color: var(--umb-text-muted);
      font-size: 12px;
      margin-top: 2px;
    }

    /* Hidden file input */
    input[type="file"] {
      position: absolute;
      width: 1px;
      height: 1px;
      opacity: 0;
      pointer-events: none;
    }

    /* Buttons */
    .btn-container {
      text-align: center;
      margin-top: 24px;
    }

    uui-button[look="primary"] {
      --uui-button-background-color: var(--umb-accent);
      --uui-button-background-color-hover: var(--umb-accent-hover);
      box-shadow: 0 2px 8px rgba(43,195,123,0.25);
      transition: transform .15s, box-shadow .2s;
    }

    uui-button[look="primary"]:hover {
      transform: translateY(-1px);
      box-shadow: 0 4px 16px rgba(43,195,123,0.35);
    }

    uui-button[look="primary"]:active {
      transform: translateY(0);
    }

    /* Requirements */
    .req-section {
      margin-bottom: 24px;
    }

    .req-section:last-child {
      margin-bottom: 0;
    }

    .req-label {
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      color: var(--umb-text-muted);
      margin-bottom: 14px;
    }

    .req-grid {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr;
      gap: 12px;
    }

    @media (max-width: 768px) {
      .req-grid {
        grid-template-columns: 1fr;
      }
    }

    .req-item {
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 16px;
      transition: border-color .2s, box-shadow .2s;
    }

    .req-item:hover {
      border-color: rgba(27,38,79,0.2);
      box-shadow: var(--umb-shadow-sm);
    }

    .req-item code {
      display: inline-block;
      background: var(--umb-blue);
      color: #fff;
      font-size: 12px;
      font-weight: 700;
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
      padding: 3px 8px;
      border-radius: 5px;
      margin-bottom: 8px;
    }

    .req-item .desc {
      font-size: 13px;
      color: var(--umb-text-muted);
      line-height: 1.45;
    }

    .req-item .examples {
      margin-top: 8px;
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }

    .req-item .examples span {
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
      font-size: 11px;
      background: var(--umb-code-bg);
      color: var(--umb-text-muted);
      padding: 2px 7px;
      border-radius: 4px;
    }

    /* Media tips */
    .media-tips {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    @media (max-width: 768px) {
      .media-tips {
        grid-template-columns: 1fr;
      }
    }

    .media-tip {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      background: var(--umb-surface);
      border: 1px solid var(--umb-border);
      border-radius: var(--umb-radius);
      padding: 14px 16px;
      font-size: 13px;
      color: var(--umb-text-muted);
      line-height: 1.45;
    }

    .media-tip .tip-icon {
      width: 22px;
      height: 22px;
      border-radius: 6px;
      background: rgba(27,38,79,0.06);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      color: var(--umb-blue-light);
      font-size: 12px;
    }

    .media-tip code {
      background: var(--umb-code-bg);
      padding: 1px 5px;
      border-radius: 3px;
      font-size: 11px;
      font-family: 'SF Mono', 'Fira Code', Consolas, monospace;
    }

    /* Results */
    .results-summary {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      margin-bottom: 1.5em;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from { opacity: 0; transform: translateX(-20px); }
      to { opacity: 1; transform: translateX(0); }
    }

    .badge {
      padding: 8px 16px;
      border-radius: 20px;
      border: 1px solid;
      font-size: 13px;
    }

    .badge-total {
      background-color: #f5f5f5;
      border-color: #ddd;
    }

    .badge-success {
      background-color: #d4edda;
      border-color: #28a745;
      color: #155724;
    }

    .badge-failed {
      background-color: #f8d7da;
      border-color: #dc3545;
      color: #721c24;
    }

    /* Footer */
    .plugin-footer {
      margin-top: 48px;
      text-align: center;
      animation: fadeUp .6s ease .15s both;
    }

    .plugin-footer .divider {
      width: 48px;
      height: 2px;
      background: var(--umb-border);
      margin: 0 auto 20px;
      border-radius: 2px;
    }

    .plugin-footer .brand-link {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      text-decoration: none;
      color: var(--umb-text-muted);
      font-size: 13px;
      font-weight: 400;
      padding: 10px 20px;
      border-radius: var(--umb-radius);
      transition: background .2s, color .2s, transform .15s;
    }

    .plugin-footer .brand-link:hover {
      background: rgba(27,38,79,0.04);
      color: var(--umb-blue);
      transform: translateY(-1px);
    }

    .plugin-footer .brand-link .heart {
      color: var(--umb-danger);
      font-size: 14px;
    }

    .plugin-footer .brand-link img {
      height: 18px;
      width: auto;
      opacity: 0.7;
      transition: opacity .2s;
    }

    .plugin-footer .brand-link:hover img {
      opacity: 1;
    }

    /* Loading state */
    .loading-state {
      margin-bottom: 20px;
      text-align: center;
    }

    .loading-state p {
      color: #666;
      margin-top: 10px;
    }

    /* Accessibility */
    uui-button:focus-visible,
    input:focus-visible {
      outline: 2px solid var(--umb-blue);
      outline-offset: 2px;
    }
  `;
ue([
  De()
], T.prototype, "dashboardState", 2);
T = ue([
  qe("bulk-upload-dashboard")
], T);
const Je = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  get BulkUploadDashboardElement() {
    return T;
  }
}, Symbol.toStringTag, { value: "Module" })), Qe = (r, e) => {
  r.consumeContext(he, async (t) => {
    if (!t) return;
    const i = t.getOpenApiConfiguration();
    y.setConfig({
      baseUrl: i.base,
      token: i.token,
      credentials: i.credentials
    });
  }), e.registerMany(ge);
};
export {
  T as BulkUploadDashboardElement,
  Qe as onInit
};
//# sourceMappingURL=bulkupload.js.map
