const ut = {
  type: "section",
  alias: "BulkUpload.Section",
  name: "Bulk Upload",
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload"
  }
}, pt = {
  type: "dashboard",
  alias: "BulkUpload.Dashboard",
  name: "Bulk Upload Dashboard",
  element: () => Promise.resolve().then(() => Wt),
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
}, ft = [ut, pt];
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const O = globalThis, L = O.ShadowRoot && (O.ShadyCSS === void 0 || O.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, q = Symbol(), Z = /* @__PURE__ */ new WeakMap();
let rt = class {
  constructor(t, e, s) {
    if (this._$cssResult$ = !0, s !== q) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = t, this.t = e;
  }
  get styleSheet() {
    let t = this.o;
    const e = this.t;
    if (L && t === void 0) {
      const s = e !== void 0 && e.length === 1;
      s && (t = Z.get(e)), t === void 0 && ((this.o = t = new CSSStyleSheet()).replaceSync(this.cssText), s && Z.set(e, t));
    }
    return t;
  }
  toString() {
    return this.cssText;
  }
};
const mt = (o) => new rt(typeof o == "string" ? o : o + "", void 0, q), $t = (o, ...t) => {
  const e = o.length === 1 ? o[0] : t.reduce((s, i, n) => s + ((r) => {
    if (r._$cssResult$ === !0) return r.cssText;
    if (typeof r == "number") return r;
    throw Error("Value passed to 'css' function must be a 'css' function result: " + r + ". Use 'unsafeCSS' to pass non-literal values, but take care to ensure page security.");
  })(i) + o[n + 1], o[0]);
  return new rt(e, o, q);
}, gt = (o, t) => {
  if (L) o.adoptedStyleSheets = t.map((e) => e instanceof CSSStyleSheet ? e : e.styleSheet);
  else for (const e of t) {
    const s = document.createElement("style"), i = O.litNonce;
    i !== void 0 && s.setAttribute("nonce", i), s.textContent = e.cssText, o.appendChild(s);
  }
}, J = L ? (o) => o : (o) => o instanceof CSSStyleSheet ? ((t) => {
  let e = "";
  for (const s of t.cssRules) e += s.cssText;
  return mt(e);
})(o) : o;
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const { is: bt, defineProperty: vt, getOwnPropertyDescriptor: yt, getOwnPropertyNames: _t, getOwnPropertySymbols: At, getPrototypeOf: xt } = Object, g = globalThis, K = g.trustedTypes, wt = K ? K.emptyScript : "", F = g.reactiveElementPolyfillSupport, S = (o, t) => o, N = { toAttribute(o, t) {
  switch (t) {
    case Boolean:
      o = o ? wt : null;
      break;
    case Object:
    case Array:
      o = o == null ? o : JSON.stringify(o);
  }
  return o;
}, fromAttribute(o, t) {
  let e = o;
  switch (t) {
    case Boolean:
      e = o !== null;
      break;
    case Number:
      e = o === null ? null : Number(o);
      break;
    case Object:
    case Array:
      try {
        e = JSON.parse(o);
      } catch {
        e = null;
      }
  }
  return e;
} }, V = (o, t) => !bt(o, t), G = { attribute: !0, type: String, converter: N, reflect: !1, useDefault: !1, hasChanged: V };
Symbol.metadata ?? (Symbol.metadata = Symbol("metadata")), g.litPropertyMetadata ?? (g.litPropertyMetadata = /* @__PURE__ */ new WeakMap());
let A = class extends HTMLElement {
  static addInitializer(t) {
    this._$Ei(), (this.l ?? (this.l = [])).push(t);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(t, e = G) {
    if (e.state && (e.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(t) && ((e = Object.create(e)).wrapped = !0), this.elementProperties.set(t, e), !e.noAccessor) {
      const s = Symbol(), i = this.getPropertyDescriptor(t, s, e);
      i !== void 0 && vt(this.prototype, t, i);
    }
  }
  static getPropertyDescriptor(t, e, s) {
    const { get: i, set: n } = yt(this.prototype, t) ?? { get() {
      return this[e];
    }, set(r) {
      this[e] = r;
    } };
    return { get: i, set(r) {
      const l = i == null ? void 0 : i.call(this);
      n == null || n.call(this, r), this.requestUpdate(t, l, s);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(t) {
    return this.elementProperties.get(t) ?? G;
  }
  static _$Ei() {
    if (this.hasOwnProperty(S("elementProperties"))) return;
    const t = xt(this);
    t.finalize(), t.l !== void 0 && (this.l = [...t.l]), this.elementProperties = new Map(t.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(S("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(S("properties"))) {
      const e = this.properties, s = [..._t(e), ...At(e)];
      for (const i of s) this.createProperty(i, e[i]);
    }
    const t = this[Symbol.metadata];
    if (t !== null) {
      const e = litPropertyMetadata.get(t);
      if (e !== void 0) for (const [s, i] of e) this.elementProperties.set(s, i);
    }
    this._$Eh = /* @__PURE__ */ new Map();
    for (const [e, s] of this.elementProperties) {
      const i = this._$Eu(e, s);
      i !== void 0 && this._$Eh.set(i, e);
    }
    this.elementStyles = this.finalizeStyles(this.styles);
  }
  static finalizeStyles(t) {
    const e = [];
    if (Array.isArray(t)) {
      const s = new Set(t.flat(1 / 0).reverse());
      for (const i of s) e.unshift(J(i));
    } else t !== void 0 && e.push(J(t));
    return e;
  }
  static _$Eu(t, e) {
    const s = e.attribute;
    return s === !1 ? void 0 : typeof s == "string" ? s : typeof t == "string" ? t.toLowerCase() : void 0;
  }
  constructor() {
    super(), this._$Ep = void 0, this.isUpdatePending = !1, this.hasUpdated = !1, this._$Em = null, this._$Ev();
  }
  _$Ev() {
    var t;
    this._$ES = new Promise((e) => this.enableUpdating = e), this._$AL = /* @__PURE__ */ new Map(), this._$E_(), this.requestUpdate(), (t = this.constructor.l) == null || t.forEach((e) => e(this));
  }
  addController(t) {
    var e;
    (this._$EO ?? (this._$EO = /* @__PURE__ */ new Set())).add(t), this.renderRoot !== void 0 && this.isConnected && ((e = t.hostConnected) == null || e.call(t));
  }
  removeController(t) {
    var e;
    (e = this._$EO) == null || e.delete(t);
  }
  _$E_() {
    const t = /* @__PURE__ */ new Map(), e = this.constructor.elementProperties;
    for (const s of e.keys()) this.hasOwnProperty(s) && (t.set(s, this[s]), delete this[s]);
    t.size > 0 && (this._$Ep = t);
  }
  createRenderRoot() {
    const t = this.shadowRoot ?? this.attachShadow(this.constructor.shadowRootOptions);
    return gt(t, this.constructor.elementStyles), t;
  }
  connectedCallback() {
    var t;
    this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this.enableUpdating(!0), (t = this._$EO) == null || t.forEach((e) => {
      var s;
      return (s = e.hostConnected) == null ? void 0 : s.call(e);
    });
  }
  enableUpdating(t) {
  }
  disconnectedCallback() {
    var t;
    (t = this._$EO) == null || t.forEach((e) => {
      var s;
      return (s = e.hostDisconnected) == null ? void 0 : s.call(e);
    });
  }
  attributeChangedCallback(t, e, s) {
    this._$AK(t, s);
  }
  _$ET(t, e) {
    var n;
    const s = this.constructor.elementProperties.get(t), i = this.constructor._$Eu(t, s);
    if (i !== void 0 && s.reflect === !0) {
      const r = (((n = s.converter) == null ? void 0 : n.toAttribute) !== void 0 ? s.converter : N).toAttribute(e, s.type);
      this._$Em = t, r == null ? this.removeAttribute(i) : this.setAttribute(i, r), this._$Em = null;
    }
  }
  _$AK(t, e) {
    var n, r;
    const s = this.constructor, i = s._$Eh.get(t);
    if (i !== void 0 && this._$Em !== i) {
      const l = s.getPropertyOptions(i), a = typeof l.converter == "function" ? { fromAttribute: l.converter } : ((n = l.converter) == null ? void 0 : n.fromAttribute) !== void 0 ? l.converter : N;
      this._$Em = i;
      const h = a.fromAttribute(e, l.type);
      this[i] = h ?? ((r = this._$Ej) == null ? void 0 : r.get(i)) ?? h, this._$Em = null;
    }
  }
  requestUpdate(t, e, s, i = !1, n) {
    var r;
    if (t !== void 0) {
      const l = this.constructor;
      if (i === !1 && (n = this[t]), s ?? (s = l.getPropertyOptions(t)), !((s.hasChanged ?? V)(n, e) || s.useDefault && s.reflect && n === ((r = this._$Ej) == null ? void 0 : r.get(t)) && !this.hasAttribute(l._$Eu(t, s)))) return;
      this.C(t, e, s);
    }
    this.isUpdatePending === !1 && (this._$ES = this._$EP());
  }
  C(t, e, { useDefault: s, reflect: i, wrapped: n }, r) {
    s && !(this._$Ej ?? (this._$Ej = /* @__PURE__ */ new Map())).has(t) && (this._$Ej.set(t, r ?? e ?? this[t]), n !== !0 || r !== void 0) || (this._$AL.has(t) || (this.hasUpdated || s || (e = void 0), this._$AL.set(t, e)), i === !0 && this._$Em !== t && (this._$Eq ?? (this._$Eq = /* @__PURE__ */ new Set())).add(t));
  }
  async _$EP() {
    this.isUpdatePending = !0;
    try {
      await this._$ES;
    } catch (e) {
      Promise.reject(e);
    }
    const t = this.scheduleUpdate();
    return t != null && await t, !this.isUpdatePending;
  }
  scheduleUpdate() {
    return this.performUpdate();
  }
  performUpdate() {
    var s;
    if (!this.isUpdatePending) return;
    if (!this.hasUpdated) {
      if (this.renderRoot ?? (this.renderRoot = this.createRenderRoot()), this._$Ep) {
        for (const [n, r] of this._$Ep) this[n] = r;
        this._$Ep = void 0;
      }
      const i = this.constructor.elementProperties;
      if (i.size > 0) for (const [n, r] of i) {
        const { wrapped: l } = r, a = this[n];
        l !== !0 || this._$AL.has(n) || a === void 0 || this.C(n, void 0, r, a);
      }
    }
    let t = !1;
    const e = this._$AL;
    try {
      t = this.shouldUpdate(e), t ? (this.willUpdate(e), (s = this._$EO) == null || s.forEach((i) => {
        var n;
        return (n = i.hostUpdate) == null ? void 0 : n.call(i);
      }), this.update(e)) : this._$EM();
    } catch (i) {
      throw t = !1, this._$EM(), i;
    }
    t && this._$AE(e);
  }
  willUpdate(t) {
  }
  _$AE(t) {
    var e;
    (e = this._$EO) == null || e.forEach((s) => {
      var i;
      return (i = s.hostUpdated) == null ? void 0 : i.call(s);
    }), this.hasUpdated || (this.hasUpdated = !0, this.firstUpdated(t)), this.updated(t);
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
  shouldUpdate(t) {
    return !0;
  }
  update(t) {
    this._$Eq && (this._$Eq = this._$Eq.forEach((e) => this._$ET(e, this[e]))), this._$EM();
  }
  updated(t) {
  }
  firstUpdated(t) {
  }
};
A.elementStyles = [], A.shadowRootOptions = { mode: "open" }, A[S("elementProperties")] = /* @__PURE__ */ new Map(), A[S("finalized")] = /* @__PURE__ */ new Map(), F == null || F({ ReactiveElement: A }), (g.reactiveElementVersions ?? (g.reactiveElementVersions = [])).push("2.1.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const E = globalThis, Q = (o) => o, B = E.trustedTypes, X = B ? B.createPolicy("lit-html", { createHTML: (o) => o }) : void 0, at = "$lit$", $ = `lit$${Math.random().toFixed(9).slice(2)}$`, lt = "?" + $, Ct = `<${lt}>`, _ = document, P = () => _.createComment(""), U = (o) => o === null || typeof o != "object" && typeof o != "function", W = Array.isArray, St = (o) => W(o) || typeof (o == null ? void 0 : o[Symbol.iterator]) == "function", H = `[ 	
\f\r]`, C = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, Y = /-->/g, tt = />/g, b = RegExp(`>|${H}(?:([^\\s"'>=/]+)(${H}*=${H}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), et = /'/g, st = /"/g, ct = /^(?:script|style|textarea|title)$/i, Et = (o) => (t, ...e) => ({ _$litType$: o, strings: t, values: e }), p = Et(1), x = Symbol.for("lit-noChange"), c = Symbol.for("lit-nothing"), it = /* @__PURE__ */ new WeakMap(), v = _.createTreeWalker(_, 129);
function dt(o, t) {
  if (!W(o) || !o.hasOwnProperty("raw")) throw Error("invalid template strings array");
  return X !== void 0 ? X.createHTML(t) : t;
}
const Mt = (o, t) => {
  const e = o.length - 1, s = [];
  let i, n = t === 2 ? "<svg>" : t === 3 ? "<math>" : "", r = C;
  for (let l = 0; l < e; l++) {
    const a = o[l];
    let h, u, d = -1, f = 0;
    for (; f < a.length && (r.lastIndex = f, u = r.exec(a), u !== null); ) f = r.lastIndex, r === C ? u[1] === "!--" ? r = Y : u[1] !== void 0 ? r = tt : u[2] !== void 0 ? (ct.test(u[2]) && (i = RegExp("</" + u[2], "g")), r = b) : u[3] !== void 0 && (r = b) : r === b ? u[0] === ">" ? (r = i ?? C, d = -1) : u[1] === void 0 ? d = -2 : (d = r.lastIndex - u[2].length, h = u[1], r = u[3] === void 0 ? b : u[3] === '"' ? st : et) : r === st || r === et ? r = b : r === Y || r === tt ? r = C : (r = b, i = void 0);
    const m = r === b && o[l + 1].startsWith("/>") ? " " : "";
    n += r === C ? a + Ct : d >= 0 ? (s.push(h), a.slice(0, d) + at + a.slice(d) + $ + m) : a + $ + (d === -2 ? l : m);
  }
  return [dt(o, n + (o[e] || "<?>") + (t === 2 ? "</svg>" : t === 3 ? "</math>" : "")), s];
};
class T {
  constructor({ strings: t, _$litType$: e }, s) {
    let i;
    this.parts = [];
    let n = 0, r = 0;
    const l = t.length - 1, a = this.parts, [h, u] = Mt(t, e);
    if (this.el = T.createElement(h, s), v.currentNode = this.el.content, e === 2 || e === 3) {
      const d = this.el.content.firstChild;
      d.replaceWith(...d.childNodes);
    }
    for (; (i = v.nextNode()) !== null && a.length < l; ) {
      if (i.nodeType === 1) {
        if (i.hasAttributes()) for (const d of i.getAttributeNames()) if (d.endsWith(at)) {
          const f = u[r++], m = i.getAttribute(d).split($), I = /([.?@])?(.*)/.exec(f);
          a.push({ type: 1, index: n, name: I[2], strings: m, ctor: I[1] === "." ? Ut : I[1] === "?" ? Tt : I[1] === "@" ? Rt : z }), i.removeAttribute(d);
        } else d.startsWith($) && (a.push({ type: 6, index: n }), i.removeAttribute(d));
        if (ct.test(i.tagName)) {
          const d = i.textContent.split($), f = d.length - 1;
          if (f > 0) {
            i.textContent = B ? B.emptyScript : "";
            for (let m = 0; m < f; m++) i.append(d[m], P()), v.nextNode(), a.push({ type: 2, index: ++n });
            i.append(d[f], P());
          }
        }
      } else if (i.nodeType === 8) if (i.data === lt) a.push({ type: 2, index: n });
      else {
        let d = -1;
        for (; (d = i.data.indexOf($, d + 1)) !== -1; ) a.push({ type: 7, index: n }), d += $.length - 1;
      }
      n++;
    }
  }
  static createElement(t, e) {
    const s = _.createElement("template");
    return s.innerHTML = t, s;
  }
}
function w(o, t, e = o, s) {
  var r, l;
  if (t === x) return t;
  let i = s !== void 0 ? (r = e._$Co) == null ? void 0 : r[s] : e._$Cl;
  const n = U(t) ? void 0 : t._$litDirective$;
  return (i == null ? void 0 : i.constructor) !== n && ((l = i == null ? void 0 : i._$AO) == null || l.call(i, !1), n === void 0 ? i = void 0 : (i = new n(o), i._$AT(o, e, s)), s !== void 0 ? (e._$Co ?? (e._$Co = []))[s] = i : e._$Cl = i), i !== void 0 && (t = w(o, i._$AS(o, t.values), i, s)), t;
}
class Pt {
  constructor(t, e) {
    this._$AV = [], this._$AN = void 0, this._$AD = t, this._$AM = e;
  }
  get parentNode() {
    return this._$AM.parentNode;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  u(t) {
    const { el: { content: e }, parts: s } = this._$AD, i = ((t == null ? void 0 : t.creationScope) ?? _).importNode(e, !0);
    v.currentNode = i;
    let n = v.nextNode(), r = 0, l = 0, a = s[0];
    for (; a !== void 0; ) {
      if (r === a.index) {
        let h;
        a.type === 2 ? h = new k(n, n.nextSibling, this, t) : a.type === 1 ? h = new a.ctor(n, a.name, a.strings, this, t) : a.type === 6 && (h = new kt(n, this, t)), this._$AV.push(h), a = s[++l];
      }
      r !== (a == null ? void 0 : a.index) && (n = v.nextNode(), r++);
    }
    return v.currentNode = _, i;
  }
  p(t) {
    let e = 0;
    for (const s of this._$AV) s !== void 0 && (s.strings !== void 0 ? (s._$AI(t, s, e), e += s.strings.length - 2) : s._$AI(t[e])), e++;
  }
}
class k {
  get _$AU() {
    var t;
    return ((t = this._$AM) == null ? void 0 : t._$AU) ?? this._$Cv;
  }
  constructor(t, e, s, i) {
    this.type = 2, this._$AH = c, this._$AN = void 0, this._$AA = t, this._$AB = e, this._$AM = s, this.options = i, this._$Cv = (i == null ? void 0 : i.isConnected) ?? !0;
  }
  get parentNode() {
    let t = this._$AA.parentNode;
    const e = this._$AM;
    return e !== void 0 && (t == null ? void 0 : t.nodeType) === 11 && (t = e.parentNode), t;
  }
  get startNode() {
    return this._$AA;
  }
  get endNode() {
    return this._$AB;
  }
  _$AI(t, e = this) {
    t = w(this, t, e), U(t) ? t === c || t == null || t === "" ? (this._$AH !== c && this._$AR(), this._$AH = c) : t !== this._$AH && t !== x && this._(t) : t._$litType$ !== void 0 ? this.$(t) : t.nodeType !== void 0 ? this.T(t) : St(t) ? this.k(t) : this._(t);
  }
  O(t) {
    return this._$AA.parentNode.insertBefore(t, this._$AB);
  }
  T(t) {
    this._$AH !== t && (this._$AR(), this._$AH = this.O(t));
  }
  _(t) {
    this._$AH !== c && U(this._$AH) ? this._$AA.nextSibling.data = t : this.T(_.createTextNode(t)), this._$AH = t;
  }
  $(t) {
    var n;
    const { values: e, _$litType$: s } = t, i = typeof s == "number" ? this._$AC(t) : (s.el === void 0 && (s.el = T.createElement(dt(s.h, s.h[0]), this.options)), s);
    if (((n = this._$AH) == null ? void 0 : n._$AD) === i) this._$AH.p(e);
    else {
      const r = new Pt(i, this), l = r.u(this.options);
      r.p(e), this.T(l), this._$AH = r;
    }
  }
  _$AC(t) {
    let e = it.get(t.strings);
    return e === void 0 && it.set(t.strings, e = new T(t)), e;
  }
  k(t) {
    W(this._$AH) || (this._$AH = [], this._$AR());
    const e = this._$AH;
    let s, i = 0;
    for (const n of t) i === e.length ? e.push(s = new k(this.O(P()), this.O(P()), this, this.options)) : s = e[i], s._$AI(n), i++;
    i < e.length && (this._$AR(s && s._$AB.nextSibling, i), e.length = i);
  }
  _$AR(t = this._$AA.nextSibling, e) {
    var s;
    for ((s = this._$AP) == null ? void 0 : s.call(this, !1, !0, e); t !== this._$AB; ) {
      const i = Q(t).nextSibling;
      Q(t).remove(), t = i;
    }
  }
  setConnected(t) {
    var e;
    this._$AM === void 0 && (this._$Cv = t, (e = this._$AP) == null || e.call(this, t));
  }
}
class z {
  get tagName() {
    return this.element.tagName;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  constructor(t, e, s, i, n) {
    this.type = 1, this._$AH = c, this._$AN = void 0, this.element = t, this.name = e, this._$AM = i, this.options = n, s.length > 2 || s[0] !== "" || s[1] !== "" ? (this._$AH = Array(s.length - 1).fill(new String()), this.strings = s) : this._$AH = c;
  }
  _$AI(t, e = this, s, i) {
    const n = this.strings;
    let r = !1;
    if (n === void 0) t = w(this, t, e, 0), r = !U(t) || t !== this._$AH && t !== x, r && (this._$AH = t);
    else {
      const l = t;
      let a, h;
      for (t = n[0], a = 0; a < n.length - 1; a++) h = w(this, l[s + a], e, a), h === x && (h = this._$AH[a]), r || (r = !U(h) || h !== this._$AH[a]), h === c ? t = c : t !== c && (t += (h ?? "") + n[a + 1]), this._$AH[a] = h;
    }
    r && !i && this.j(t);
  }
  j(t) {
    t === c ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, t ?? "");
  }
}
class Ut extends z {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(t) {
    this.element[this.name] = t === c ? void 0 : t;
  }
}
class Tt extends z {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(t) {
    this.element.toggleAttribute(this.name, !!t && t !== c);
  }
}
class Rt extends z {
  constructor(t, e, s, i, n) {
    super(t, e, s, i, n), this.type = 5;
  }
  _$AI(t, e = this) {
    if ((t = w(this, t, e, 0) ?? c) === x) return;
    const s = this._$AH, i = t === c && s !== c || t.capture !== s.capture || t.once !== s.once || t.passive !== s.passive, n = t !== c && (s === c || i);
    i && this.element.removeEventListener(this.name, this, s), n && this.element.addEventListener(this.name, this, t), this._$AH = t;
  }
  handleEvent(t) {
    var e;
    typeof this._$AH == "function" ? this._$AH.call(((e = this.options) == null ? void 0 : e.host) ?? this.element, t) : this._$AH.handleEvent(t);
  }
}
class kt {
  constructor(t, e, s) {
    this.element = t, this.type = 6, this._$AN = void 0, this._$AM = e, this.options = s;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(t) {
    w(this, t);
  }
}
const D = E.litHtmlPolyfillSupport;
D == null || D(T, k), (E.litHtmlVersions ?? (E.litHtmlVersions = [])).push("3.3.2");
const It = (o, t, e) => {
  const s = (e == null ? void 0 : e.renderBefore) ?? t;
  let i = s._$litPart$;
  if (i === void 0) {
    const n = (e == null ? void 0 : e.renderBefore) ?? null;
    s._$litPart$ = i = new k(t.insertBefore(P(), n), n, void 0, e ?? {});
  }
  return i._$AI(o), i;
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const y = globalThis;
class M extends A {
  constructor() {
    super(...arguments), this.renderOptions = { host: this }, this._$Do = void 0;
  }
  createRenderRoot() {
    var e;
    const t = super.createRenderRoot();
    return (e = this.renderOptions).renderBefore ?? (e.renderBefore = t.firstChild), t;
  }
  update(t) {
    const e = this.render();
    this.hasUpdated || (this.renderOptions.isConnected = this.isConnected), super.update(t), this._$Do = It(e, this.renderRoot, this.renderOptions);
  }
  connectedCallback() {
    var t;
    super.connectedCallback(), (t = this._$Do) == null || t.setConnected(!0);
  }
  disconnectedCallback() {
    var t;
    super.disconnectedCallback(), (t = this._$Do) == null || t.setConnected(!1);
  }
  render() {
    return x;
  }
}
var nt;
M._$litElement$ = !0, M.finalized = !0, (nt = y.litElementHydrateSupport) == null || nt.call(y, { LitElement: M });
const j = y.litElementPolyfillSupport;
j == null || j({ LitElement: M });
(y.litElementVersions ?? (y.litElementVersions = [])).push("4.2.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const Ot = (o) => (t, e) => {
  e !== void 0 ? e.addInitializer(() => {
    customElements.define(o, t);
  }) : customElements.define(o, t);
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const Nt = { attribute: !0, type: String, converter: N, reflect: !1, hasChanged: V }, Bt = (o = Nt, t, e) => {
  const { kind: s, metadata: i } = e;
  let n = globalThis.litPropertyMetadata.get(i);
  if (n === void 0 && globalThis.litPropertyMetadata.set(i, n = /* @__PURE__ */ new Map()), s === "setter" && ((o = Object.create(o)).wrapped = !0), n.set(e.name, o), s === "accessor") {
    const { name: r } = e;
    return { set(l) {
      const a = t.get.call(this);
      t.set.call(this, l), this.requestUpdate(r, a, o, !0, l);
    }, init(l) {
      return l !== void 0 && this.C(r, void 0, o, l), l;
    } };
  }
  if (s === "setter") {
    const { name: r } = e;
    return function(l) {
      const a = this[r];
      t.call(this, l), this.requestUpdate(r, a, o, !0, l);
    };
  }
  throw Error("Unsupported decorator location: " + s);
};
function zt(o) {
  return (t, e) => typeof e == "object" ? Bt(o, t, e) : ((s, i, n) => {
    const r = i.hasOwnProperty(n);
    return i.constructor.createProperty(n, s), r ? Object.getOwnPropertyDescriptor(i, n) : void 0;
  })(o, t, e);
}
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
function Ft(o) {
  return zt({ ...o, state: !0, attribute: !1 });
}
class Ht {
  /**
   * Imports content from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importContent(t) {
    if (!t)
      throw new Error("File is required for content import");
    try {
      return await this.post(
        "/Umbraco/backoffice/Api/BulkUpload/ImportAll",
        t
      );
    } catch (e) {
      throw new Error("Content import failed: " + e.message);
    }
  }
  /**
   * Imports media from a CSV or ZIP file
   * @param file - The CSV or ZIP file to import
   * @returns Promise resolving to import results
   */
  async importMedia(t) {
    if (!t)
      throw new Error("File is required for media import");
    try {
      return await this.post(
        "/Umbraco/backoffice/Api/MediaImport/ImportMedia",
        t
      );
    } catch (e) {
      throw new Error("Media import failed: " + e.message);
    }
  }
  /**
   * Exports content import results to CSV or ZIP
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportContentResults(t) {
    if (!t || !Array.isArray(t))
      throw new Error("Results array is required for export");
    try {
      return await this.postForBlob(
        "/Umbraco/backoffice/Api/BulkUpload/ExportResults",
        t
      );
    } catch (e) {
      throw new Error("Export failed: " + e.message);
    }
  }
  /**
   * Exports media import results to CSV
   * @param results - Array of import result objects
   * @returns Promise resolving to Response object for file download
   */
  async exportMediaResults(t) {
    if (!t || !Array.isArray(t))
      throw new Error("Results array is required for export");
    try {
      return await this.postForBlob(
        "/Umbraco/backoffice/Api/MediaImport/ExportResults",
        t
      );
    } catch (e) {
      throw new Error("Export failed: " + e.message);
    }
  }
  /**
   * Validates a file before import
   * @param file - The file to validate
   * @param options - Validation options
   * @returns Validation result with valid flag and errors array
   */
  validateFile(t, e = {}) {
    var h;
    const s = [];
    if (!t)
      return s.push("No file selected"), { valid: !1, errors: s };
    const i = e.acceptedTypes || [".csv", ".zip"], n = ((h = t.name.split(".").pop()) == null ? void 0 : h.toLowerCase()) || "";
    i.some((u) => (u.startsWith(".") ? u.slice(1) : u).toLowerCase() === n) || s.push(`File type .${n} is not accepted. Accepted types: ${i.join(", ")}`);
    const l = e.maxSizeInMB || 100, a = l * 1024 * 1024;
    return t.size > a && s.push(`File size (${Math.round(t.size / 1024 / 1024)}MB) exceeds maximum (${l}MB)`), {
      valid: s.length === 0,
      errors: s
    };
  }
  /**
   * Internal: POST request for JSON data
   */
  async post(t, e) {
    let s;
    const i = {};
    if (e instanceof File) {
      const l = new FormData();
      l.append("file", e), s = l;
    } else
      s = JSON.stringify(e), i["Content-Type"] = "application/json";
    const n = await fetch(t, {
      method: "POST",
      body: s,
      headers: i
    });
    if (!n.ok)
      throw new Error(`HTTP ${n.status}: ${n.statusText}`);
    return {
      data: await n.json(),
      status: n.status,
      headers: n.headers
    };
  }
  /**
   * Internal: POST request for blob/file downloads
   */
  async postForBlob(t, e) {
    const s = await fetch(t, {
      method: "POST",
      body: JSON.stringify(e),
      headers: {
        "Content-Type": "application/json"
      }
    });
    if (!s.ok)
      throw new Error(`HTTP ${s.status}: ${s.statusText}`);
    return s;
  }
}
class Dt {
  constructor(t, e, s) {
    if (!t)
      throw new Error("API client is required");
    if (!e)
      throw new Error("Notification handler is required");
    this.apiClient = t, this.notify = e, this.onStateChange = s || null, this.state = this.createInitialState();
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
  setActiveTab(t) {
    if (t !== "content" && t !== "media")
      throw new Error('Invalid tab name. Must be "content" or "media"');
    this.state.activeTab = t, this.emitStateChange();
  }
  /**
   * Sets content file and file element
   */
  setContentFile(t, e) {
    this.state.content.file = t, this.state.content.fileElement = e || null, this.emitStateChange();
  }
  /**
   * Sets media file and file element
   */
  setMediaFile(t, e) {
    this.state.media.file = t, this.state.media.fileElement = e || null, this.emitStateChange();
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
    const t = this.state.content.file;
    if (!t)
      return this.notify({
        type: "warning",
        headline: "No File Selected",
        message: "Please select a CSV or ZIP file to import."
      }), null;
    const e = this.apiClient.validateFile(t, {
      acceptedTypes: [".csv", ".zip"],
      maxSizeInMB: 100
    });
    if (!e.valid)
      return this.notify({
        type: "error",
        headline: "Invalid File",
        message: e.errors.join(", ")
      }), null;
    this.state.content.loading = !0, this.state.content.results = null, this.emitStateChange();
    try {
      const s = await this.apiClient.importContent(t);
      this.clearContentFile(), this.state.content.results = s.data;
      const i = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let n;
      return i.total === 0 ? n = "No content items to import." : i.failed === 0 ? n = `All ${i.total} content items imported successfully.` : i.success === 0 ? n = `All ${i.total} content items failed to import.` : n = `${i.success} of ${i.total} content items imported successfully. ${i.failed} failed.`, this.notify({
        type: i.failed > 0 ? "warning" : "success",
        headline: "Content Import Complete",
        message: n
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
    const t = this.state.media.file;
    if (!t)
      return this.notify({
        type: "warning",
        headline: "No File Selected",
        message: "Please select a CSV or ZIP file to import."
      }), null;
    const e = this.apiClient.validateFile(t, {
      acceptedTypes: [".csv", ".zip"],
      maxSizeInMB: 100
    });
    if (!e.valid)
      return this.notify({
        type: "error",
        headline: "Invalid File",
        message: e.errors.join(", ")
      }), null;
    this.state.media.loading = !0, this.state.media.results = null, this.emitStateChange();
    try {
      const s = await this.apiClient.importMedia(t);
      this.clearMediaFile(), this.state.media.results = s.data;
      const i = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let n;
      return i.total === 0 ? n = "No media items to import." : i.failed === 0 ? n = `All ${i.total} media items imported successfully.` : i.success === 0 ? n = `All ${i.total} media items failed to import.` : n = `${i.success} of ${i.total} media items imported successfully. ${i.failed} failed.`, this.notify({
        type: i.failed > 0 ? "warning" : "success",
        headline: "Media Import Complete",
        message: n
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
    const t = this.state.content.results;
    if (!t || !t.results)
      return this.notify({
        type: "warning",
        headline: "No Results",
        message: "No results available to export."
      }), null;
    try {
      const e = await this.apiClient.exportContentResults(t.results);
      return this.notify({
        type: "success",
        headline: "Export Successful",
        message: "Results exported successfully."
      }), e;
    } catch (e) {
      throw this.notify({
        type: "error",
        headline: "Export Failed",
        message: "Failed to export results."
      }), e;
    }
  }
  /**
   * Exports media import results to CSV
   */
  async exportMediaResults() {
    const t = this.state.media.results;
    if (!t || !t.results)
      return this.notify({
        type: "warning",
        headline: "No Results",
        message: "No results available to export."
      }), null;
    try {
      const e = await this.apiClient.exportMediaResults(t.results);
      return this.notify({
        type: "success",
        headline: "Export Successful",
        message: "Results exported successfully."
      }), e;
    } catch (e) {
      throw this.notify({
        type: "error",
        headline: "Export Failed",
        message: "Failed to export results."
      }), e;
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
function jt(o) {
  if (!o || o === 0) return "0 Bytes";
  const t = 1024, e = ["Bytes", "KB", "MB", "GB", "TB"], s = Math.floor(Math.log(o) / Math.log(t));
  return `${Math.round(o / Math.pow(t, s) * 100) / 100} ${e[s]}`;
}
function Lt(o, t) {
  const e = document.createElement("a");
  e.href = window.URL.createObjectURL(o), e.download = t, e.click(), setTimeout(() => {
    window.URL.revokeObjectURL(e.href);
  }, 100);
}
async function ot(o, t) {
  if (!o || !o.ok) {
    console.error("Invalid response for file download");
    return;
  }
  const e = await o.blob();
  let s = t;
  const i = o.headers.get("content-type"), n = o.headers.get("content-disposition");
  if (n) {
    const r = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(n);
    r != null && r[1] && (s = r[1].replace(/['"]/g, ""));
  }
  (!s || s === t) && i && i.indexOf("application/zip") !== -1 && (s = t.replace(/\.csv$/, ".zip")), Lt(e, s);
}
var qt = Object.defineProperty, Vt = Object.getOwnPropertyDescriptor, ht = (o, t, e, s) => {
  for (var i = s > 1 ? void 0 : s ? Vt(t, e) : t, n = o.length - 1, r; n >= 0; n--)
    (r = o[n]) && (i = (s ? r(t, e, i) : r(i)) || i);
  return s && i && qt(t, e, i), i;
};
let R = class extends M {
  constructor() {
    super();
    const o = new Ht();
    this.service = new Dt(
      o,
      this.handleNotification.bind(this),
      this.handleStateChange.bind(this)
    ), this.dashboardState = this.service.getState();
  }
  handleNotification(o) {
    this.dispatchEvent(new CustomEvent("notification", {
      detail: { notification: o },
      bubbles: !0,
      composed: !0
    }));
  }
  handleStateChange(o) {
    this.dashboardState = { ...o };
  }
  handleContentFileChange(o) {
    var s;
    const t = o.target, e = (s = t.files) == null ? void 0 : s[0];
    e && this.service.setContentFile(e, t);
  }
  handleMediaFileChange(o) {
    var s;
    const t = o.target, e = (s = t.files) == null ? void 0 : s[0];
    e && this.service.setMediaFile(e, t);
  }
  async handleContentImport() {
    await this.service.importContent();
  }
  async handleMediaImport() {
    await this.service.importMedia();
  }
  async handleContentExport() {
    const o = await this.service.exportContentResults();
    o && await ot(o, "content-results.csv");
  }
  async handleMediaExport() {
    const o = await this.service.exportMediaResults();
    o && await ot(o, "media-results.csv");
  }
  render() {
    const { activeTab: o, content: t, media: e } = this.dashboardState;
    return p`
      <uui-box>
        <div slot="header" class="dashboard-header">
          <h2>Bulk Upload</h2>
        </div>

        <!-- Tab Navigation -->
        <uui-tab-group>
          <uui-tab
            label="Content Import"
            ?active=${o === "content"}
            @click=${() => this.service.setActiveTab("content")}>
            Content Import
          </uui-tab>
          <uui-tab
            label="Media Import"
            ?active=${o === "media"}
            @click=${() => this.service.setActiveTab("media")}>
            Media Import
          </uui-tab>
        </uui-tab-group>

        <!-- Content Import Panel -->
        ${o === "content" ? p`
          <div class="tab-panel">
            ${this.renderInfoBox("content")}
            ${this.renderUploadSection("content", t)}
            ${t.loading ? this.renderLoadingState("content") : c}
            ${t.results && !t.loading ? this.renderResults("content", t.results) : c}
          </div>
        ` : c}

        <!-- Media Import Panel -->
        ${o === "media" ? p`
          <div class="tab-panel">
            ${this.renderInfoBox("media")}
            ${this.renderUploadSection("media", e)}
            ${e.loading ? this.renderLoadingState("media") : c}
            ${e.results && !e.loading ? this.renderResults("media", e.results) : c}
          </div>
        ` : c}
      </uui-box>
    `;
  }
  renderInfoBox(o) {
    return o === "content" ? p`
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
  renderUploadSection(o, t) {
    const e = o === "content", s = e ? "content-file-input" : "media-file-input";
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
            ?disabled=${t.loading}
            @change=${e ? this.handleContentFileChange : this.handleMediaFileChange}
            class="file-input" />

          ${t.file && !t.loading ? p`
            <div class="file-info">
              <span class="file-icon">📄</span>
              <div class="file-details">
                <strong>${t.file.name}</strong>
                <span class="file-size">(${jt(t.file.size)})</span>
              </div>
            </div>
          ` : c}

          <div class="button-group">
            <uui-button
              label=${e ? "Import Content" : "Import Media"}
              look="primary"
              color="positive"
              ?disabled=${!t.file || t.loading}
              @click=${e ? this.handleContentImport : this.handleMediaImport}>
              ${t.loading ? "Processing..." : `▲ Import ${e ? "Content" : "Media"}`}
            </uui-button>
            ${t.file && !t.loading ? p`
              <uui-button
                label="Clear File"
                look="outline"
                @click=${() => e ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            ` : c}
          </div>
        </div>
      </uui-box>
    `;
  }
  renderLoadingState(o) {
    return p`
      <div class="loading-state">
        <uui-loader-bar></uui-loader-bar>
        <p>Importing ${o}, please wait...</p>
      </div>
    `;
  }
  renderResults(o, t) {
    const e = {
      total: t.totalCount || 0,
      success: t.successCount || 0,
      failed: t.failureCount || 0,
      successRate: t.totalCount > 0 ? Math.round(t.successCount / t.totalCount * 100) : 0
    };
    return p`
      <uui-box headline="Import Results" class="results-section">
        <div class="results-content">
          <!-- Statistics -->
          <div class="stats-grid">
            <div class="stat-card stat-total">
              <div class="stat-label">Total</div>
              <div class="stat-value">${e.total}</div>
            </div>
            <div class="stat-card stat-success">
              <div class="stat-label">Success</div>
              <div class="stat-value">${e.success}</div>
            </div>
            <div class="stat-card stat-failed">
              <div class="stat-label">Failed</div>
              <div class="stat-value">${e.failed}</div>
            </div>
            <div class="stat-card stat-rate">
              <div class="stat-label">Success Rate</div>
              <div class="stat-value">${e.successRate}%</div>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="export-section">
            <uui-button
              label="Export Results"
              look="outline"
              color="default"
              @click=${o === "content" ? this.handleContentExport : this.handleMediaExport}>
              ⬇ Export Results
            </uui-button>
            <uui-button
              label="Clear Results"
              look="outline"
              color="default"
              @click=${() => o === "content" ? this.service.clearContentResults() : this.service.clearMediaResults()}>
              Clear Results
            </uui-button>
          </div>

          <!-- Results Table -->
          ${t.results && t.results.length > 0 ? p`
            <div class="results-table-container">
              <table class="results-table">
                <thead>
                  <tr>
                    <th>Status</th>
                    <th>Name</th>
                    ${o === "content" ? p`<th>Doc Type</th>` : p`<th>Media Type</th>`}
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  ${t.results.map((s) => p`
                    <tr class=${s.success ? "success" : "failed"}>
                      <td>${s.success ? "✅" : "❌"}</td>
                      <td>${s.name || "-"}</td>
                      <td>${o === "content" ? s.docTypeAlias : s.mediaTypeAlias || "-"}</td>
                      <td class="message-cell">${s.errorMessage || "Success"}</td>
                    </tr>
                  `)}
                </tbody>
              </table>
            </div>
          ` : c}
        </div>
      </uui-box>
    `;
  }
};
R.styles = $t`
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

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 15px;
      margin-bottom: 20px;
    }

    .stat-card {
      padding: 15px;
      border-radius: 8px;
      text-align: center;
    }

    .stat-total {
      background-color: #e3f2fd;
      border-left: 4px solid #1976d2;
    }

    .stat-success {
      background-color: #e8f5e9;
      border-left: 4px solid #4caf50;
    }

    .stat-failed {
      background-color: #ffebee;
      border-left: 4px solid #f44336;
    }

    .stat-rate {
      background-color: #f3e5f5;
      border-left: 4px solid #9c27b0;
    }

    .stat-label {
      font-size: 0.875rem;
      color: #666;
      margin-bottom: 5px;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 600;
    }

    .export-section {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }

    .results-table-container {
      overflow-x: auto;
      border: 1px solid #ddd;
      border-radius: 4px;
    }

    .results-table {
      width: 100%;
      border-collapse: collapse;
    }

    .results-table th,
    .results-table td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    .results-table th {
      background-color: #f5f5f5;
      font-weight: 600;
    }

    .results-table tr.success {
      background-color: #f1f8f4;
    }

    .results-table tr.failed {
      background-color: #fef5f5;
    }

    .message-cell {
      max-width: 300px;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `;
ht([
  Ft()
], R.prototype, "dashboardState", 2);
R = ht([
  Ot("bulk-upload-dashboard")
], R);
const Wt = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  get BulkUploadDashboardElement() {
    return R;
  }
}, Symbol.toStringTag, { value: "Module" })), Kt = (o, t) => {
  t.registerMany(ft);
};
export {
  R as BulkUploadDashboardElement,
  Kt as onInit
};
//# sourceMappingURL=bulkupload.js.map
