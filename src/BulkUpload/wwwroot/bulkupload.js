import { UMB_AUTH_CONTEXT as pe } from "@umbraco-cms/backoffice/auth";
const fe = {
  type: "section",
  alias: "BulkUpload.Section",
  name: "Bulk Upload",
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload"
  }
}, me = {
  type: "dashboard",
  alias: "BulkUpload.Dashboard",
  name: "Bulk Upload Dashboard",
  element: () => Promise.resolve().then(() => Ke),
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
}, ge = [fe, me];
class $e {
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
const b = new $e();
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const N = globalThis, q = N.ShadowRoot && (N.ShadyCSS === void 0 || N.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, V = Symbol(), J = /* @__PURE__ */ new WeakMap();
let ae = class {
  constructor(e, t, s) {
    if (this._$cssResult$ = !0, s !== V) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = e, this.t = t;
  }
  get styleSheet() {
    let e = this.o;
    const t = this.t;
    if (q && e === void 0) {
      const s = t !== void 0 && t.length === 1;
      s && (e = J.get(t)), e === void 0 && ((this.o = e = new CSSStyleSheet()).replaceSync(this.cssText), s && J.set(t, e));
    }
    return e;
  }
  toString() {
    return this.cssText;
  }
};
const ye = (n) => new ae(typeof n == "string" ? n : n + "", void 0, V), be = (n, ...e) => {
  const t = n.length === 1 ? n[0] : e.reduce((s, i, o) => s + ((r) => {
    if (r._$cssResult$ === !0) return r.cssText;
    if (typeof r == "number") return r;
    throw Error("Value passed to 'css' function must be a 'css' function result: " + r + ". Use 'unsafeCSS' to pass non-literal values, but take care to ensure page security.");
  })(i) + n[o + 1], n[0]);
  return new ae(t, n, V);
}, ve = (n, e) => {
  if (q) n.adoptedStyleSheets = e.map((t) => t instanceof CSSStyleSheet ? t : t.styleSheet);
  else for (const t of e) {
    const s = document.createElement("style"), i = N.litNonce;
    i !== void 0 && s.setAttribute("nonce", i), s.textContent = t.cssText, n.appendChild(s);
  }
}, K = q ? (n) => n : (n) => n instanceof CSSStyleSheet ? ((e) => {
  let t = "";
  for (const s of e.cssRules) t += s.cssText;
  return ye(t);
})(n) : n;
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const { is: _e, defineProperty: Ae, getOwnPropertyDescriptor: Ce, getOwnPropertyNames: we, getOwnPropertySymbols: xe, getPrototypeOf: Ee } = Object, $ = globalThis, G = $.trustedTypes, Se = G ? G.emptyScript : "", z = $.reactiveElementPolyfillSupport, S = (n, e) => n, B = { toAttribute(n, e) {
  switch (e) {
    case Boolean:
      n = n ? Se : null;
      break;
    case Object:
    case Array:
      n = n == null ? n : JSON.stringify(n);
  }
  return n;
}, fromAttribute(n, e) {
  let t = n;
  switch (e) {
    case Boolean:
      t = n !== null;
      break;
    case Number:
      t = n === null ? null : Number(n);
      break;
    case Object:
    case Array:
      try {
        t = JSON.parse(n);
      } catch {
        t = null;
      }
  }
  return t;
} }, W = (n, e) => !_e(n, e), X = { attribute: !0, type: String, converter: B, reflect: !1, useDefault: !1, hasChanged: W };
Symbol.metadata ?? (Symbol.metadata = Symbol("metadata")), $.litPropertyMetadata ?? ($.litPropertyMetadata = /* @__PURE__ */ new WeakMap());
let C = class extends HTMLElement {
  static addInitializer(e) {
    this._$Ei(), (this.l ?? (this.l = [])).push(e);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(e, t = X) {
    if (t.state && (t.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(e) && ((t = Object.create(t)).wrapped = !0), this.elementProperties.set(e, t), !t.noAccessor) {
      const s = Symbol(), i = this.getPropertyDescriptor(e, s, t);
      i !== void 0 && Ae(this.prototype, e, i);
    }
  }
  static getPropertyDescriptor(e, t, s) {
    const { get: i, set: o } = Ce(this.prototype, e) ?? { get() {
      return this[t];
    }, set(r) {
      this[t] = r;
    } };
    return { get: i, set(r) {
      const l = i == null ? void 0 : i.call(this);
      o == null || o.call(this, r), this.requestUpdate(e, l, s);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(e) {
    return this.elementProperties.get(e) ?? X;
  }
  static _$Ei() {
    if (this.hasOwnProperty(S("elementProperties"))) return;
    const e = Ee(this);
    e.finalize(), e.l !== void 0 && (this.l = [...e.l]), this.elementProperties = new Map(e.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(S("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(S("properties"))) {
      const t = this.properties, s = [...we(t), ...xe(t)];
      for (const i of s) this.createProperty(i, t[i]);
    }
    const e = this[Symbol.metadata];
    if (e !== null) {
      const t = litPropertyMetadata.get(e);
      if (t !== void 0) for (const [s, i] of t) this.elementProperties.set(s, i);
    }
    this._$Eh = /* @__PURE__ */ new Map();
    for (const [t, s] of this.elementProperties) {
      const i = this._$Eu(t, s);
      i !== void 0 && this._$Eh.set(i, t);
    }
    this.elementStyles = this.finalizeStyles(this.styles);
  }
  static finalizeStyles(e) {
    const t = [];
    if (Array.isArray(e)) {
      const s = new Set(e.flat(1 / 0).reverse());
      for (const i of s) t.unshift(K(i));
    } else e !== void 0 && t.push(K(e));
    return t;
  }
  static _$Eu(e, t) {
    const s = t.attribute;
    return s === !1 ? void 0 : typeof s == "string" ? s : typeof e == "string" ? e.toLowerCase() : void 0;
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
    for (const s of t.keys()) this.hasOwnProperty(s) && (e.set(s, this[s]), delete this[s]);
    e.size > 0 && (this._$Ep = e);
  }
  createRenderRoot() {
    const e = this.shadowRoot ?? this.attachShadow(this.constructor.shadowRootOptions);
    return ve(e, this.constructor.elementStyles), e;
  }
  connectedCallback() {
    var e;
    this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this.enableUpdating(!0), (e = this._$EO) == null || e.forEach((t) => {
      var s;
      return (s = t.hostConnected) == null ? void 0 : s.call(t);
    });
  }
  enableUpdating(e) {
  }
  disconnectedCallback() {
    var e;
    (e = this._$EO) == null || e.forEach((t) => {
      var s;
      return (s = t.hostDisconnected) == null ? void 0 : s.call(t);
    });
  }
  attributeChangedCallback(e, t, s) {
    this._$AK(e, s);
  }
  _$ET(e, t) {
    var o;
    const s = this.constructor.elementProperties.get(e), i = this.constructor._$Eu(e, s);
    if (i !== void 0 && s.reflect === !0) {
      const r = (((o = s.converter) == null ? void 0 : o.toAttribute) !== void 0 ? s.converter : B).toAttribute(t, s.type);
      this._$Em = e, r == null ? this.removeAttribute(i) : this.setAttribute(i, r), this._$Em = null;
    }
  }
  _$AK(e, t) {
    var o, r;
    const s = this.constructor, i = s._$Eh.get(e);
    if (i !== void 0 && this._$Em !== i) {
      const l = s.getPropertyOptions(i), a = typeof l.converter == "function" ? { fromAttribute: l.converter } : ((o = l.converter) == null ? void 0 : o.fromAttribute) !== void 0 ? l.converter : B;
      this._$Em = i;
      const c = a.fromAttribute(t, l.type);
      this[i] = c ?? ((r = this._$Ej) == null ? void 0 : r.get(i)) ?? c, this._$Em = null;
    }
  }
  requestUpdate(e, t, s, i = !1, o) {
    var r;
    if (e !== void 0) {
      const l = this.constructor;
      if (i === !1 && (o = this[e]), s ?? (s = l.getPropertyOptions(e)), !((s.hasChanged ?? W)(o, t) || s.useDefault && s.reflect && o === ((r = this._$Ej) == null ? void 0 : r.get(e)) && !this.hasAttribute(l._$Eu(e, s)))) return;
      this.C(e, t, s);
    }
    this.isUpdatePending === !1 && (this._$ES = this._$EP());
  }
  C(e, t, { useDefault: s, reflect: i, wrapped: o }, r) {
    s && !(this._$Ej ?? (this._$Ej = /* @__PURE__ */ new Map())).has(e) && (this._$Ej.set(e, r ?? t ?? this[e]), o !== !0 || r !== void 0) || (this._$AL.has(e) || (this.hasUpdated || s || (t = void 0), this._$AL.set(e, t)), i === !0 && this._$Em !== e && (this._$Eq ?? (this._$Eq = /* @__PURE__ */ new Set())).add(e));
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
    var s;
    if (!this.isUpdatePending) return;
    if (!this.hasUpdated) {
      if (this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this._$Ep) {
        for (const [o, r] of this._$Ep) this[o] = r;
        this._$Ep = void 0;
      }
      const i = this.constructor.elementProperties;
      if (i.size > 0) for (const [o, r] of i) {
        const { wrapped: l } = r, a = this[o];
        l !== !0 || this._$AL.has(o) || a === void 0 || this.C(o, void 0, r, a);
      }
    }
    let e = !1;
    const t = this._$AL;
    try {
      e = this.shouldUpdate(t), e ? (this.willUpdate(t), (s = this._$EO) == null || s.forEach((i) => {
        var o;
        return (o = i.hostUpdate) == null ? void 0 : o.call(i);
      }), this.update(t)) : this._$EM();
    } catch (i) {
      throw e = !1, this._$EM(), i;
    }
    e && this._$AE(t);
  }
  willUpdate(e) {
  }
  _$AE(e) {
    var t;
    (t = this._$EO) == null || t.forEach((s) => {
      var i;
      return (i = s.hostUpdated) == null ? void 0 : i.call(s);
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
C.elementStyles = [], C.shadowRootOptions = { mode: "open" }, C[S("elementProperties")] = /* @__PURE__ */ new Map(), C[S("finalized")] = /* @__PURE__ */ new Map(), z == null || z({ ReactiveElement: C }), ($.reactiveElementVersions ?? ($.reactiveElementVersions = [])).push("2.1.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const M = globalThis, Q = (n) => n, H = M.trustedTypes, Y = H ? H.createPolicy("lit-html", { createHTML: (n) => n }) : void 0, le = "$lit$", g = `lit$${Math.random().toFixed(9).slice(2)}$`, ce = "?" + g, Me = `<${ce}>`, A = document, P = () => A.createComment(""), T = (n) => n === null || typeof n != "object" && typeof n != "function", Z = Array.isArray, Ue = (n) => Z(n) || typeof (n == null ? void 0 : n[Symbol.iterator]) == "function", D = `[ 	
\f\r]`, E = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, ee = /-->/g, te = />/g, y = RegExp(`>|${D}(?:([^\\s"'>=/]+)(${D}*=${D}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), se = /'/g, ie = /"/g, de = /^(?:script|style|textarea|title)$/i, Pe = (n) => (e, ...t) => ({ _$litType$: n, strings: e, values: t }), p = Pe(1), w = Symbol.for("lit-noChange"), h = Symbol.for("lit-nothing"), ne = /* @__PURE__ */ new WeakMap(), v = A.createTreeWalker(A, 129);
function he(n, e) {
  if (!Z(n) || !n.hasOwnProperty("raw")) throw Error("invalid template strings array");
  return Y !== void 0 ? Y.createHTML(e) : e;
}
const Te = (n, e) => {
  const t = n.length - 1, s = [];
  let i, o = e === 2 ? "<svg>" : e === 3 ? "<math>" : "", r = E;
  for (let l = 0; l < t; l++) {
    const a = n[l];
    let c, u, d = -1, f = 0;
    for (; f < a.length && (r.lastIndex = f, u = r.exec(a), u !== null); ) f = r.lastIndex, r === E ? u[1] === "!--" ? r = ee : u[1] !== void 0 ? r = te : u[2] !== void 0 ? (de.test(u[2]) && (i = RegExp("</" + u[2], "g")), r = y) : u[3] !== void 0 && (r = y) : r === y ? u[0] === ">" ? (r = i ?? E, d = -1) : u[1] === void 0 ? d = -2 : (d = r.lastIndex - u[2].length, c = u[1], r = u[3] === void 0 ? y : u[3] === '"' ? ie : se) : r === ie || r === se ? r = y : r === ee || r === te ? r = E : (r = y, i = void 0);
    const m = r === y && n[l + 1].startsWith("/>") ? " " : "";
    o += r === E ? a + Me : d >= 0 ? (s.push(c), a.slice(0, d) + le + a.slice(d) + g + m) : a + g + (d === -2 ? l : m);
  }
  return [he(n, o + (n[t] || "<?>") + (e === 2 ? "</svg>" : e === 3 ? "</math>" : "")), s];
};
class k {
  constructor({ strings: e, _$litType$: t }, s) {
    let i;
    this.parts = [];
    let o = 0, r = 0;
    const l = e.length - 1, a = this.parts, [c, u] = Te(e, t);
    if (this.el = k.createElement(c, s), v.currentNode = this.el.content, t === 2 || t === 3) {
      const d = this.el.content.firstChild;
      d.replaceWith(...d.childNodes);
    }
    for (; (i = v.nextNode()) !== null && a.length < l; ) {
      if (i.nodeType === 1) {
        if (i.hasAttributes()) for (const d of i.getAttributeNames()) if (d.endsWith(le)) {
          const f = u[r++], m = i.getAttribute(d).split(g), O = /([.?@])?(.*)/.exec(f);
          a.push({ type: 1, index: o, name: O[2], strings: m, ctor: O[1] === "." ? Re : O[1] === "?" ? Ie : O[1] === "@" ? Oe : F }), i.removeAttribute(d);
        } else d.startsWith(g) && (a.push({ type: 6, index: o }), i.removeAttribute(d));
        if (de.test(i.tagName)) {
          const d = i.textContent.split(g), f = d.length - 1;
          if (f > 0) {
            i.textContent = H ? H.emptyScript : "";
            for (let m = 0; m < f; m++) i.append(d[m], P()), v.nextNode(), a.push({ type: 2, index: ++o });
            i.append(d[f], P());
          }
        }
      } else if (i.nodeType === 8) if (i.data === ce) a.push({ type: 2, index: o });
      else {
        let d = -1;
        for (; (d = i.data.indexOf(g, d + 1)) !== -1; ) a.push({ type: 7, index: o }), d += g.length - 1;
      }
      o++;
    }
  }
  static createElement(e, t) {
    const s = A.createElement("template");
    return s.innerHTML = e, s;
  }
}
function x(n, e, t = n, s) {
  var r, l;
  if (e === w) return e;
  let i = s !== void 0 ? (r = t._$Co) == null ? void 0 : r[s] : t._$Cl;
  const o = T(e) ? void 0 : e._$litDirective$;
  return (i == null ? void 0 : i.constructor) !== o && ((l = i == null ? void 0 : i._$AO) == null || l.call(i, !1), o === void 0 ? i = void 0 : (i = new o(n), i._$AT(n, t, s)), s !== void 0 ? (t._$Co ?? (t._$Co = []))[s] = i : t._$Cl = i), i !== void 0 && (e = x(n, i._$AS(n, e.values), i, s)), e;
}
class ke {
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
    const { el: { content: t }, parts: s } = this._$AD, i = ((e == null ? void 0 : e.creationScope) ?? A).importNode(t, !0);
    v.currentNode = i;
    let o = v.nextNode(), r = 0, l = 0, a = s[0];
    for (; a !== void 0; ) {
      if (r === a.index) {
        let c;
        a.type === 2 ? c = new I(o, o.nextSibling, this, e) : a.type === 1 ? c = new a.ctor(o, a.name, a.strings, this, e) : a.type === 6 && (c = new Ne(o, this, e)), this._$AV.push(c), a = s[++l];
      }
      r !== (a == null ? void 0 : a.index) && (o = v.nextNode(), r++);
    }
    return v.currentNode = A, i;
  }
  p(e) {
    let t = 0;
    for (const s of this._$AV) s !== void 0 && (s.strings !== void 0 ? (s._$AI(e, s, t), t += s.strings.length - 2) : s._$AI(e[t])), t++;
  }
}
class I {
  get _$AU() {
    var e;
    return ((e = this._$AM) == null ? void 0 : e._$AU) ?? this._$Cv;
  }
  constructor(e, t, s, i) {
    this.type = 2, this._$AH = h, this._$AN = void 0, this._$AA = e, this._$AB = t, this._$AM = s, this.options = i, this._$Cv = (i == null ? void 0 : i.isConnected) ?? !0;
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
    e = x(this, e, t), T(e) ? e === h || e == null || e === "" ? (this._$AH !== h && this._$AR(), this._$AH = h) : e !== this._$AH && e !== w && this._(e) : e._$litType$ !== void 0 ? this.$(e) : e.nodeType !== void 0 ? this.T(e) : Ue(e) ? this.k(e) : this._(e);
  }
  O(e) {
    return this._$AA.parentNode.insertBefore(e, this._$AB);
  }
  T(e) {
    this._$AH !== e && (this._$AR(), this._$AH = this.O(e));
  }
  _(e) {
    this._$AH !== h && T(this._$AH) ? this._$AA.nextSibling.data = e : this.T(A.createTextNode(e)), this._$AH = e;
  }
  $(e) {
    var o;
    const { values: t, _$litType$: s } = e, i = typeof s == "number" ? this._$AC(e) : (s.el === void 0 && (s.el = k.createElement(he(s.h, s.h[0]), this.options)), s);
    if (((o = this._$AH) == null ? void 0 : o._$AD) === i) this._$AH.p(t);
    else {
      const r = new ke(i, this), l = r.u(this.options);
      r.p(t), this.T(l), this._$AH = r;
    }
  }
  _$AC(e) {
    let t = ne.get(e.strings);
    return t === void 0 && ne.set(e.strings, t = new k(e)), t;
  }
  k(e) {
    Z(this._$AH) || (this._$AH = [], this._$AR());
    const t = this._$AH;
    let s, i = 0;
    for (const o of e) i === t.length ? t.push(s = new I(this.O(P()), this.O(P()), this, this.options)) : s = t[i], s._$AI(o), i++;
    i < t.length && (this._$AR(s && s._$AB.nextSibling, i), t.length = i);
  }
  _$AR(e = this._$AA.nextSibling, t) {
    var s;
    for ((s = this._$AP) == null ? void 0 : s.call(this, !1, !0, t); e !== this._$AB; ) {
      const i = Q(e).nextSibling;
      Q(e).remove(), e = i;
    }
  }
  setConnected(e) {
    var t;
    this._$AM === void 0 && (this._$Cv = e, (t = this._$AP) == null || t.call(this, e));
  }
}
class F {
  get tagName() {
    return this.element.tagName;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  constructor(e, t, s, i, o) {
    this.type = 1, this._$AH = h, this._$AN = void 0, this.element = e, this.name = t, this._$AM = i, this.options = o, s.length > 2 || s[0] !== "" || s[1] !== "" ? (this._$AH = Array(s.length - 1).fill(new String()), this.strings = s) : this._$AH = h;
  }
  _$AI(e, t = this, s, i) {
    const o = this.strings;
    let r = !1;
    if (o === void 0) e = x(this, e, t, 0), r = !T(e) || e !== this._$AH && e !== w, r && (this._$AH = e);
    else {
      const l = e;
      let a, c;
      for (e = o[0], a = 0; a < o.length - 1; a++) c = x(this, l[s + a], t, a), c === w && (c = this._$AH[a]), r || (r = !T(c) || c !== this._$AH[a]), c === h ? e = h : e !== h && (e += (c ?? "") + o[a + 1]), this._$AH[a] = c;
    }
    r && !i && this.j(e);
  }
  j(e) {
    e === h ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, e ?? "");
  }
}
class Re extends F {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(e) {
    this.element[this.name] = e === h ? void 0 : e;
  }
}
class Ie extends F {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(e) {
    this.element.toggleAttribute(this.name, !!e && e !== h);
  }
}
class Oe extends F {
  constructor(e, t, s, i, o) {
    super(e, t, s, i, o), this.type = 5;
  }
  _$AI(e, t = this) {
    if ((e = x(this, e, t, 0) ?? h) === w) return;
    const s = this._$AH, i = e === h && s !== h || e.capture !== s.capture || e.once !== s.once || e.passive !== s.passive, o = e !== h && (s === h || i);
    i && this.element.removeEventListener(this.name, this, s), o && this.element.addEventListener(this.name, this, e), this._$AH = e;
  }
  handleEvent(e) {
    var t;
    typeof this._$AH == "function" ? this._$AH.call(((t = this.options) == null ? void 0 : t.host) ?? this.element, e) : this._$AH.handleEvent(e);
  }
}
class Ne {
  constructor(e, t, s) {
    this.element = e, this.type = 6, this._$AN = void 0, this._$AM = t, this.options = s;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(e) {
    x(this, e);
  }
}
const j = M.litHtmlPolyfillSupport;
j == null || j(k, I), (M.litHtmlVersions ?? (M.litHtmlVersions = [])).push("3.3.2");
const Be = (n, e, t) => {
  const s = (t == null ? void 0 : t.renderBefore) ?? e;
  let i = s._$litPart$;
  if (i === void 0) {
    const o = (t == null ? void 0 : t.renderBefore) ?? null;
    s._$litPart$ = i = new I(e.insertBefore(P(), o), o, void 0, t ?? {});
  }
  return i._$AI(n), i;
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const _ = globalThis;
class U extends C {
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
    this.hasUpdated || (this.renderOptions.isConnected = this.isConnected), super.update(e), this._$Do = Be(t, this.renderRoot, this.renderOptions);
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
    return w;
  }
}
var re;
U._$litElement$ = !0, U.finalized = !0, (re = _.litElementHydrateSupport) == null || re.call(_, { LitElement: U });
const L = _.litElementPolyfillSupport;
L == null || L({ LitElement: U });
(_.litElementVersions ?? (_.litElementVersions = [])).push("4.2.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const He = (n) => (e, t) => {
  t !== void 0 ? t.addInitializer(() => {
    customElements.define(n, e);
  }) : customElements.define(n, e);
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const Fe = { attribute: !0, type: String, converter: B, reflect: !1, hasChanged: W }, ze = (n = Fe, e, t) => {
  const { kind: s, metadata: i } = t;
  let o = globalThis.litPropertyMetadata.get(i);
  if (o === void 0 && globalThis.litPropertyMetadata.set(i, o = /* @__PURE__ */ new Map()), s === "setter" && ((n = Object.create(n)).wrapped = !0), o.set(t.name, n), s === "accessor") {
    const { name: r } = t;
    return { set(l) {
      const a = e.get.call(this);
      e.set.call(this, l), this.requestUpdate(r, a, n, !0, l);
    }, init(l) {
      return l !== void 0 && this.C(r, void 0, n, l), l;
    } };
  }
  if (s === "setter") {
    const { name: r } = t;
    return function(l) {
      const a = this[r];
      e.call(this, l), this.requestUpdate(r, a, n, !0, l);
    };
  }
  throw Error("Unsupported decorator location: " + s);
};
function De(n) {
  return (e, t) => typeof t == "object" ? ze(n, e, t) : ((s, i, o) => {
    const r = i.hasOwnProperty(o);
    return i.constructor.createProperty(o, s), r ? Object.getOwnPropertyDescriptor(i, o) : void 0;
  })(n, e, t);
}
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
function je(n) {
  return De({ ...n, state: !0, attribute: !1 });
}
class Le {
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
    var c;
    const s = [];
    if (!e)
      return s.push("No file selected"), { valid: !1, errors: s };
    const i = t.acceptedTypes || [".csv", ".zip"], o = ((c = e.name.split(".").pop()) == null ? void 0 : c.toLowerCase()) || "";
    i.some((u) => (u.startsWith(".") ? u.slice(1) : u).toLowerCase() === o) || s.push(`File type .${o} is not accepted. Accepted types: ${i.join(", ")}`);
    const l = t.maxSizeInMB || 100, a = l * 1024 * 1024;
    return e.size > a && s.push(`File size (${Math.round(e.size / 1024 / 1024)}MB) exceeds maximum (${l}MB)`), {
      valid: s.length === 0,
      errors: s
    };
  }
  /**
   * Internal: POST request for JSON data
   */
  async post(e, t) {
    let s;
    const i = {}, o = await b.getAuthHeaders();
    if (Object.assign(i, o), t instanceof File) {
      const d = new FormData();
      d.append("file", t), s = d;
    } else
      s = JSON.stringify(t), i["Content-Type"] = "application/json";
    const r = b.getBaseUrl(), l = r ? `${r}${e}` : e, a = b.getCredentials(), c = await fetch(l, {
      method: "POST",
      body: s,
      headers: i,
      credentials: a
    });
    if (!c.ok)
      throw new Error(`HTTP ${c.status}: ${c.statusText}`);
    return {
      data: await c.json(),
      status: c.status,
      headers: c.headers
    };
  }
  /**
   * Internal: POST request for blob/file downloads
   */
  async postForBlob(e, t) {
    const i = {
      "Content-Type": "application/json",
      ...await b.getAuthHeaders()
    }, o = b.getBaseUrl(), r = o ? `${o}${e}` : e, l = b.getCredentials(), a = await fetch(r, {
      method: "POST",
      body: JSON.stringify(t),
      headers: i,
      credentials: l
    });
    if (!a.ok)
      throw new Error(`HTTP ${a.status}: ${a.statusText}`);
    return a;
  }
}
class qe {
  constructor(e, t, s) {
    if (!e)
      throw new Error("API client is required");
    if (!t)
      throw new Error("Notification handler is required");
    this.apiClient = e, this.notify = t, this.onStateChange = s || null, this.state = this.createInitialState();
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
      const s = await this.apiClient.importContent(e);
      this.clearContentFile(), this.state.content.results = s.data;
      const i = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let o;
      return i.total === 0 ? o = "No content items to import." : i.failed === 0 ? o = `All ${i.total} content items imported successfully.` : i.success === 0 ? o = `All ${i.total} content items failed to import.` : o = `${i.success} of ${i.total} content items imported successfully. ${i.failed} failed.`, this.notify({
        type: i.failed > 0 ? "warning" : "success",
        headline: "Content Import Complete",
        message: o
      }), s.data;
    } catch (s) {
      throw this.notify({
        type: "error",
        headline: "Import Failed",
        message: s.message || "An error occurred during content import."
      }), s;
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
      const s = await this.apiClient.importMedia(e);
      this.clearMediaFile(), this.state.media.results = s.data;
      const i = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let o;
      return i.total === 0 ? o = "No media items to import." : i.failed === 0 ? o = `All ${i.total} media items imported successfully.` : i.success === 0 ? o = `All ${i.total} media items failed to import.` : o = `${i.success} of ${i.total} media items imported successfully. ${i.failed} failed.`, this.notify({
        type: i.failed > 0 ? "warning" : "success",
        headline: "Media Import Complete",
        message: o
      }), s.data;
    } catch (s) {
      throw this.notify({
        type: "error",
        headline: "Import Failed",
        message: s.message || "An error occurred during media import."
      }), s;
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
function Ve(n) {
  if (!n || n === 0) return "0 Bytes";
  const e = 1024, t = ["Bytes", "KB", "MB", "GB", "TB"], s = Math.floor(Math.log(n) / Math.log(e));
  return `${Math.round(n / Math.pow(e, s) * 100) / 100} ${t[s]}`;
}
function We(n, e) {
  const t = document.createElement("a");
  t.href = window.URL.createObjectURL(n), t.download = e, t.click(), setTimeout(() => {
    window.URL.revokeObjectURL(t.href);
  }, 100);
}
async function oe(n, e) {
  if (!n || !n.ok) {
    console.error("Invalid response for file download");
    return;
  }
  const t = await n.blob();
  let s = e;
  const i = n.headers.get("content-type"), o = n.headers.get("content-disposition");
  if (o) {
    const r = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(o);
    r != null && r[1] && (s = r[1].replace(/['"]/g, ""));
  }
  (!s || s === e) && i && i.indexOf("application/zip") !== -1 && (s = e.replace(/\.csv$/, ".zip")), We(t, s);
}
var Ze = Object.defineProperty, Je = Object.getOwnPropertyDescriptor, ue = (n, e, t, s) => {
  for (var i = s > 1 ? void 0 : s ? Je(e, t) : e, o = n.length - 1, r; o >= 0; o--)
    (r = n[o]) && (i = (s ? r(e, t, i) : r(i)) || i);
  return s && i && Ze(e, t, i), i;
};
let R = class extends U {
  constructor() {
    super();
    const n = new Le();
    this.service = new qe(
      n,
      this.handleNotification.bind(this),
      this.handleStateChange.bind(this)
    ), this.dashboardState = this.service.getState();
  }
  handleNotification(n) {
    this.dispatchEvent(new CustomEvent("notification", {
      detail: { notification: n },
      bubbles: !0,
      composed: !0
    }));
  }
  handleStateChange(n) {
    this.dashboardState = { ...n };
  }
  handleContentFileChange(n) {
    var s;
    const e = n.target, t = (s = e.files) == null ? void 0 : s[0];
    t && this.service.setContentFile(t, e);
  }
  handleMediaFileChange(n) {
    var s;
    const e = n.target, t = (s = e.files) == null ? void 0 : s[0];
    t && this.service.setMediaFile(t, e);
  }
  async handleContentImport() {
    await this.service.importContent();
  }
  async handleMediaImport() {
    await this.service.importMedia();
  }
  async handleContentExport() {
    const n = await this.service.exportContentResults();
    n && await oe(n, "content-results.csv");
  }
  async handleMediaExport() {
    const n = await this.service.exportMediaResults();
    n && await oe(n, "media-results.csv");
  }
  render() {
    const { activeTab: n, content: e, media: t } = this.dashboardState;
    return p`
      <uui-box>
        <div slot="header" class="dashboard-header">
          <h2>Bulk Upload</h2>
        </div>

        <!-- Tab Navigation -->
        <uui-tab-group>
          <uui-tab
            label="Content Import"
            ?active=${n === "content"}
            @click=${() => this.service.setActiveTab("content")}>
            Content Import
          </uui-tab>
          <uui-tab
            label="Media Import"
            ?active=${n === "media"}
            @click=${() => this.service.setActiveTab("media")}>
            Media Import
          </uui-tab>
        </uui-tab-group>

        <!-- Content Import Panel -->
        ${n === "content" ? p`
          <div class="tab-panel">
            ${this.renderInfoBox("content")}
            ${this.renderUploadSection("content", e)}
            ${e.loading ? this.renderLoadingState("content") : h}
            ${e.results && !e.loading ? this.renderResults("content", e.results) : h}
          </div>
        ` : h}

        <!-- Media Import Panel -->
        ${n === "media" ? p`
          <div class="tab-panel">
            ${this.renderInfoBox("media")}
            ${this.renderUploadSection("media", t)}
            ${t.loading ? this.renderLoadingState("media") : h}
            ${t.results && !t.loading ? this.renderResults("media", t.results) : h}
          </div>
        ` : h}
      </uui-box>
    `;
  }
  renderInfoBox(n) {
    return n === "content" ? p`
        <uui-box look="outline" class="info-box">
          <div class="info-content">
            <div class="info-icon">ℹ️</div>
            <div>
              <h4>Requirements</h4>
              <ul>
                <li>The <code>parent</code>, <code>docTypeAlias</code>, and <code>name</code> columns are required</li>
                <li>Upload a ZIP file if you have media files to include with your content</li>
                <li>Use resolvers like <code>zipFileToMedia</code> to reference media files</li>
              </ul>
            </div>
          </div>
        </uui-box>
      ` : p`
        <uui-box look="outline" class="info-box">
          <div class="info-content">
            <div class="info-icon">ℹ️</div>
            <div>
              <h4>Requirements</h4>
              <ul>
                <li>The <code>fileName</code> column is required (name of media file in ZIP)</li>
                <li>Upload a ZIP file containing both CSV and media files</li>
                <li>The <code>parent</code> column can specify folder path or ID</li>
                <li>Media type is auto-detected from file extension</li>
              </ul>
            </div>
          </div>
        </uui-box>
      `;
  }
  renderUploadSection(n, e) {
    const t = n === "content", s = t ? "content-file-input" : "media-file-input";
    return p`
      <uui-box headline="Upload File" class="upload-section">
        <div class="upload-content">
          <label for=${s} class="file-label">
            Select CSV or ZIP file
          </label>
          <input
            type="file"
            id=${s}
            accept=".csv,.zip"
            ?disabled=${e.loading}
            @change=${t ? this.handleContentFileChange : this.handleMediaFileChange}
            class="file-input" />

          ${e.file && !e.loading ? p`
            <div class="file-info">
              <span class="file-icon">📄</span>
              <div class="file-details">
                <strong>${e.file.name}</strong>
                <span class="file-size">(${Ve(e.file.size)})</span>
              </div>
            </div>
          ` : h}

          <div class="button-group">
            <uui-button
              label=${t ? "Import Content" : "Import Media"}
              look="primary"
              color="positive"
              ?disabled=${!e.file || e.loading}
              @click=${t ? this.handleContentImport : this.handleMediaImport}>
              ${e.loading ? "Processing..." : `▲ Import ${t ? "Content" : "Media"}`}
            </uui-button>
            ${e.file && !e.loading ? p`
              <uui-button
                label="Clear File"
                look="outline"
                @click=${() => t ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            ` : h}
          </div>
        </div>
      </uui-box>
    `;
  }
  renderLoadingState(n) {
    return p`
      <div class="loading-state">
        <uui-loader-bar></uui-loader-bar>
        <p>Importing ${n}, please wait...</p>
      </div>
    `;
  }
  renderResults(n, e) {
    const t = {
      total: e.totalCount || 0,
      success: e.successCount || 0,
      failed: e.failureCount || 0
    };
    return p`
      <uui-box headline="Import Results" class="results-section">
        <div class="results-content">
          <!-- Summary Badges -->
          <div class="results-summary">
            <div class="badge badge-total">
              <strong>Total:</strong> ${t.total}
            </div>
            <div class="badge badge-success">
              <strong>✓ Success:</strong> ${t.success}
            </div>
            ${t.failed > 0 ? p`
              <div class="badge badge-failed">
                <strong>✗ Failed:</strong> ${t.failed}
              </div>
            ` : h}
          </div>

          <!-- Action Buttons -->
          <div class="export-section">
            <uui-button
              label="Export Results"
              look="outline"
              color="default"
              @click=${n === "content" ? this.handleContentExport : this.handleMediaExport}>
              ⬇ Export Results
            </uui-button>
            <uui-button
              label="Clear Results"
              look="outline"
              color="default"
              @click=${() => n === "content" ? this.service.clearContentResults() : this.service.clearMediaResults()}>
              Clear Results
            </uui-button>
          </div>
        </div>
      </uui-box>
    `;
  }
};
R.styles = be`
    :host {
      display: block;
      padding: 20px;
    }

    .dashboard-header h2 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    uui-tab-group {
      margin-bottom: 20px;
    }

    .tab-panel {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .info-box {
      border-color: #f0ad4e;
      background-color: #fcf8e3;
    }

    .info-content {
      padding: 1em;
      color: #8a6d3b;
      display: flex;
      align-items: start;
      gap: 10px;
    }

    .info-icon {
      font-size: 1.5em;
    }

    .info-content h4 {
      margin: 0 0 0.5em 0;
    }

    .info-content ul {
      margin: 0.5em 0;
      padding-left: 1.5em;
    }

    .info-content code {
      background-color: rgba(0, 0, 0, 0.05);
      padding: 2px 4px;
      border-radius: 3px;
    }

    .upload-content {
      padding: 1em;
    }

    .file-label {
      display: block;
      font-weight: 600;
      margin-bottom: 0.5em;
    }

    .file-input {
      margin-bottom: 1em;
      width: 100%;
    }

    .file-info {
      margin-bottom: 1em;
      padding: 0.75em;
      background-color: #f5f5f5;
      border-radius: 4px;
      border-left: 3px solid #1b264f;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .file-icon {
      font-size: 1.2em;
    }

    .file-size {
      color: #666;
      margin-left: 8px;
    }

    .button-group {
      display: flex;
      gap: 10px;
    }

    .loading-state {
      margin-bottom: 20px;
    }

    .loading-state p {
      text-align: center;
      color: #666;
      margin-top: 10px;
    }

    .results-content {
      padding: 1em;
    }

    .results-summary {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      margin-bottom: 1.5em;
    }

    .badge {
      padding: 8px 16px;
      border-radius: 20px;
      border: 1px solid;
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

    .export-section {
      display: flex;
      gap: 10px;
    }
  `;
ue([
  je()
], R.prototype, "dashboardState", 2);
R = ue([
  He("bulk-upload-dashboard")
], R);
const Ke = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  get BulkUploadDashboardElement() {
    return R;
  }
}, Symbol.toStringTag, { value: "Module" })), Ye = (n, e) => {
  n.consumeContext(pe, async (t) => {
    if (!t) return;
    const s = t.getOpenApiConfiguration();
    b.setConfig({
      baseUrl: s.base,
      token: s.token,
      credentials: s.credentials
    });
  }), e.registerMany(ge);
};
export {
  R as BulkUploadDashboardElement,
  Ye as onInit
};
//# sourceMappingURL=bulkupload.js.map
