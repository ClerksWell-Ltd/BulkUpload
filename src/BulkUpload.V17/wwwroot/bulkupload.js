import { css as U, LitElement as k, nothing as d, html as l } from "lit";
const R = {
  type: "section",
  alias: "BulkUpload.Section",
  name: "Bulk Upload",
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload"
  }
}, A = {
  type: "sectionView",
  alias: "BulkUpload.SectionView",
  name: "Bulk Upload Dashboard",
  element: () => Promise.resolve().then(() => et),
  weight: -10,
  meta: {
    label: "Bulk Upload",
    pathname: "overview",
    icon: "icon-cloud-upload"
  },
  conditions: [
    {
      alias: "Umb.Condition.SectionAlias",
      match: "BulkUpload.Section"
    }
  ]
}, T = [R, A];
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const I = (i) => (t, e) => {
  e !== void 0 ? e.addInitializer(() => {
    customElements.define(i, t);
  }) : customElements.define(i, t);
};
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const g = globalThis, $ = g.ShadowRoot && (g.ShadyCSS === void 0 || g.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, M = Symbol(), C = /* @__PURE__ */ new WeakMap();
let O = class {
  constructor(t, e, s) {
    if (this._$cssResult$ = !0, s !== M) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = t, this.t = e;
  }
  get styleSheet() {
    let t = this.o;
    const e = this.t;
    if ($ && t === void 0) {
      const s = e !== void 0 && e.length === 1;
      s && (t = C.get(e)), t === void 0 && ((this.o = t = new CSSStyleSheet()).replaceSync(this.cssText), s && C.set(e, t));
    }
    return t;
  }
  toString() {
    return this.cssText;
  }
};
const F = (i) => new O(typeof i == "string" ? i : i + "", void 0, M), z = (i, t) => {
  if ($) i.adoptedStyleSheets = t.map((e) => e instanceof CSSStyleSheet ? e : e.styleSheet);
  else for (const e of t) {
    const s = document.createElement("style"), o = g.litNonce;
    o !== void 0 && s.setAttribute("nonce", o), s.textContent = e.cssText, i.appendChild(s);
  }
}, x = $ ? (i) => i : (i) => i instanceof CSSStyleSheet ? ((t) => {
  let e = "";
  for (const s of t.cssRules) e += s.cssText;
  return F(e);
})(i) : i;
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const { is: B, defineProperty: j, getOwnPropertyDescriptor: N, getOwnPropertyNames: D, getOwnPropertySymbols: q, getPrototypeOf: L } = Object, h = globalThis, S = h.trustedTypes, V = S ? S.emptyScript : "", v = h.reactiveElementPolyfillSupport, f = (i, t) => i, b = { toAttribute(i, t) {
  switch (t) {
    case Boolean:
      i = i ? V : null;
      break;
    case Object:
    case Array:
      i = i == null ? i : JSON.stringify(i);
  }
  return i;
}, fromAttribute(i, t) {
  let e = i;
  switch (t) {
    case Boolean:
      e = i !== null;
      break;
    case Number:
      e = i === null ? null : Number(i);
      break;
    case Object:
    case Array:
      try {
        e = JSON.parse(i);
      } catch {
        e = null;
      }
  }
  return e;
} }, w = (i, t) => !B(i, t), E = { attribute: !0, type: String, converter: b, reflect: !1, useDefault: !1, hasChanged: w };
Symbol.metadata ?? (Symbol.metadata = Symbol("metadata")), h.litPropertyMetadata ?? (h.litPropertyMetadata = /* @__PURE__ */ new WeakMap());
class p extends HTMLElement {
  static addInitializer(t) {
    this._$Ei(), (this.l ?? (this.l = [])).push(t);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(t, e = E) {
    if (e.state && (e.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(t) && ((e = Object.create(e)).wrapped = !0), this.elementProperties.set(t, e), !e.noAccessor) {
      const s = Symbol(), o = this.getPropertyDescriptor(t, s, e);
      o !== void 0 && j(this.prototype, t, o);
    }
  }
  static getPropertyDescriptor(t, e, s) {
    const { get: o, set: a } = N(this.prototype, t) ?? { get() {
      return this[e];
    }, set(n) {
      this[e] = n;
    } };
    return { get: o, set(n) {
      const r = o == null ? void 0 : o.call(this);
      a == null || a.call(this, n), this.requestUpdate(t, r, s);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(t) {
    return this.elementProperties.get(t) ?? E;
  }
  static _$Ei() {
    if (this.hasOwnProperty(f("elementProperties"))) return;
    const t = L(this);
    t.finalize(), t.l !== void 0 && (this.l = [...t.l]), this.elementProperties = new Map(t.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(f("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(f("properties"))) {
      const e = this.properties, s = [...D(e), ...q(e)];
      for (const o of s) this.createProperty(o, e[o]);
    }
    const t = this[Symbol.metadata];
    if (t !== null) {
      const e = litPropertyMetadata.get(t);
      if (e !== void 0) for (const [s, o] of e) this.elementProperties.set(s, o);
    }
    this._$Eh = /* @__PURE__ */ new Map();
    for (const [e, s] of this.elementProperties) {
      const o = this._$Eu(e, s);
      o !== void 0 && this._$Eh.set(o, e);
    }
    this.elementStyles = this.finalizeStyles(this.styles);
  }
  static finalizeStyles(t) {
    const e = [];
    if (Array.isArray(t)) {
      const s = new Set(t.flat(1 / 0).reverse());
      for (const o of s) e.unshift(x(o));
    } else t !== void 0 && e.push(x(t));
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
    return z(t, this.constructor.elementStyles), t;
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
    var a;
    const s = this.constructor.elementProperties.get(t), o = this.constructor._$Eu(t, s);
    if (o !== void 0 && s.reflect === !0) {
      const n = (((a = s.converter) == null ? void 0 : a.toAttribute) !== void 0 ? s.converter : b).toAttribute(e, s.type);
      this._$Em = t, n == null ? this.removeAttribute(o) : this.setAttribute(o, n), this._$Em = null;
    }
  }
  _$AK(t, e) {
    var a, n;
    const s = this.constructor, o = s._$Eh.get(t);
    if (o !== void 0 && this._$Em !== o) {
      const r = s.getPropertyOptions(o), c = typeof r.converter == "function" ? { fromAttribute: r.converter } : ((a = r.converter) == null ? void 0 : a.fromAttribute) !== void 0 ? r.converter : b;
      this._$Em = o;
      const u = c.fromAttribute(e, r.type);
      this[o] = u ?? ((n = this._$Ej) == null ? void 0 : n.get(o)) ?? u, this._$Em = null;
    }
  }
  requestUpdate(t, e, s, o = !1, a) {
    var n;
    if (t !== void 0) {
      const r = this.constructor;
      if (o === !1 && (a = this[t]), s ?? (s = r.getPropertyOptions(t)), !((s.hasChanged ?? w)(a, e) || s.useDefault && s.reflect && a === ((n = this._$Ej) == null ? void 0 : n.get(t)) && !this.hasAttribute(r._$Eu(t, s)))) return;
      this.C(t, e, s);
    }
    this.isUpdatePending === !1 && (this._$ES = this._$EP());
  }
  C(t, e, { useDefault: s, reflect: o, wrapped: a }, n) {
    s && !(this._$Ej ?? (this._$Ej = /* @__PURE__ */ new Map())).has(t) && (this._$Ej.set(t, n ?? e ?? this[t]), a !== !0 || n !== void 0) || (this._$AL.has(t) || (this.hasUpdated || s || (e = void 0), this._$AL.set(t, e)), o === !0 && this._$Em !== t && (this._$Eq ?? (this._$Eq = /* @__PURE__ */ new Set())).add(t));
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
        for (const [a, n] of this._$Ep) this[a] = n;
        this._$Ep = void 0;
      }
      const o = this.constructor.elementProperties;
      if (o.size > 0) for (const [a, n] of o) {
        const { wrapped: r } = n, c = this[a];
        r !== !0 || this._$AL.has(a) || c === void 0 || this.C(a, void 0, n, c);
      }
    }
    let t = !1;
    const e = this._$AL;
    try {
      t = this.shouldUpdate(e), t ? (this.willUpdate(e), (s = this._$EO) == null || s.forEach((o) => {
        var a;
        return (a = o.hostUpdate) == null ? void 0 : a.call(o);
      }), this.update(e)) : this._$EM();
    } catch (o) {
      throw t = !1, this._$EM(), o;
    }
    t && this._$AE(e);
  }
  willUpdate(t) {
  }
  _$AE(t) {
    var e;
    (e = this._$EO) == null || e.forEach((s) => {
      var o;
      return (o = s.hostUpdated) == null ? void 0 : o.call(s);
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
}
p.elementStyles = [], p.shadowRootOptions = { mode: "open" }, p[f("elementProperties")] = /* @__PURE__ */ new Map(), p[f("finalized")] = /* @__PURE__ */ new Map(), v == null || v({ ReactiveElement: p }), (h.reactiveElementVersions ?? (h.reactiveElementVersions = [])).push("2.1.2");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const Z = { attribute: !0, type: String, converter: b, reflect: !1, hasChanged: w }, J = (i = Z, t, e) => {
  const { kind: s, metadata: o } = e;
  let a = globalThis.litPropertyMetadata.get(o);
  if (a === void 0 && globalThis.litPropertyMetadata.set(o, a = /* @__PURE__ */ new Map()), s === "setter" && ((i = Object.create(i)).wrapped = !0), a.set(e.name, i), s === "accessor") {
    const { name: n } = e;
    return { set(r) {
      const c = t.get.call(this);
      t.set.call(this, r), this.requestUpdate(n, c, i, !0, r);
    }, init(r) {
      return r !== void 0 && this.C(n, void 0, i, r), r;
    } };
  }
  if (s === "setter") {
    const { name: n } = e;
    return function(r) {
      const c = this[n];
      t.call(this, r), this.requestUpdate(n, c, i, !0, r);
    };
  }
  throw Error("Unsupported decorator location: " + s);
};
function K(i) {
  return (t, e) => typeof e == "object" ? J(i, t, e) : ((s, o, a) => {
    const n = o.hasOwnProperty(a);
    return o.constructor.createProperty(a, s), n ? Object.getOwnPropertyDescriptor(o, a) : void 0;
  })(i, t, e);
}
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
function W(i) {
  return K({ ...i, state: !0, attribute: !1 });
}
class H {
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
    var u;
    const s = [];
    if (!t)
      return s.push("No file selected"), { valid: !1, errors: s };
    const o = e.acceptedTypes || [".csv", ".zip"], a = ((u = t.name.split(".").pop()) == null ? void 0 : u.toLowerCase()) || "";
    o.some((y) => (y.startsWith(".") ? y.slice(1) : y).toLowerCase() === a) || s.push(`File type .${a} is not accepted. Accepted types: ${o.join(", ")}`);
    const r = e.maxSizeInMB || 100, c = r * 1024 * 1024;
    return t.size > c && s.push(`File size (${Math.round(t.size / 1024 / 1024)}MB) exceeds maximum (${r}MB)`), {
      valid: s.length === 0,
      errors: s
    };
  }
  /**
   * Internal: POST request for JSON data
   */
  async post(t, e) {
    let s;
    const o = {};
    if (e instanceof File) {
      const r = new FormData();
      r.append("file", e), s = r;
    } else
      s = JSON.stringify(e), o["Content-Type"] = "application/json";
    const a = await fetch(t, {
      method: "POST",
      body: s,
      headers: o
    });
    if (!a.ok)
      throw new Error(`HTTP ${a.status}: ${a.statusText}`);
    return {
      data: await a.json(),
      status: a.status,
      headers: a.headers
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
class G {
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
      const o = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let a;
      return o.total === 0 ? a = "No content items to import." : o.failed === 0 ? a = `All ${o.total} content items imported successfully.` : o.success === 0 ? a = `All ${o.total} content items failed to import.` : a = `${o.success} of ${o.total} content items imported successfully. ${o.failed} failed.`, this.notify({
        type: o.failed > 0 ? "warning" : "success",
        headline: "Content Import Complete",
        message: a
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
      const o = {
        total: s.data.totalCount || 0,
        success: s.data.successCount || 0,
        failed: s.data.failureCount || 0
      };
      let a;
      return o.total === 0 ? a = "No media items to import." : o.failed === 0 ? a = `All ${o.total} media items imported successfully.` : o.success === 0 ? a = `All ${o.total} media items failed to import.` : a = `${o.success} of ${o.total} media items imported successfully. ${o.failed} failed.`, this.notify({
        type: o.failed > 0 ? "warning" : "success",
        headline: "Media Import Complete",
        message: a
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
function Q(i) {
  if (!i || i === 0) return "0 Bytes";
  const t = 1024, e = ["Bytes", "KB", "MB", "GB", "TB"], s = Math.floor(Math.log(i) / Math.log(t));
  return `${Math.round(i / Math.pow(t, s) * 100) / 100} ${e[s]}`;
}
function X(i, t) {
  const e = document.createElement("a");
  e.href = window.URL.createObjectURL(i), e.download = t, e.click(), setTimeout(() => {
    window.URL.revokeObjectURL(e.href);
  }, 100);
}
async function _(i, t) {
  if (!i || !i.ok) {
    console.error("Invalid response for file download");
    return;
  }
  const e = await i.blob();
  let s = t;
  const o = i.headers.get("content-type"), a = i.headers.get("content-disposition");
  if (a) {
    const n = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(a);
    n != null && n[1] && (s = n[1].replace(/['"]/g, ""));
  }
  (!s || s === t) && o && o.indexOf("application/zip") !== -1 && (s = t.replace(/\.csv$/, ".zip")), X(e, s);
}
var Y = Object.defineProperty, tt = Object.getOwnPropertyDescriptor, P = (i, t, e, s) => {
  for (var o = s > 1 ? void 0 : s ? tt(t, e) : t, a = i.length - 1, n; a >= 0; a--)
    (n = i[a]) && (o = (s ? n(t, e, o) : n(o)) || o);
  return s && o && Y(t, e, o), o;
};
let m = class extends k {
  constructor() {
    super();
    const i = new H();
    this.service = new G(
      i,
      this.handleNotification.bind(this),
      this.handleStateChange.bind(this)
    ), this.dashboardState = this.service.getState();
  }
  handleNotification(i) {
    this.dispatchEvent(new CustomEvent("notification", {
      detail: { notification: i },
      bubbles: !0,
      composed: !0
    }));
  }
  handleStateChange(i) {
    this.dashboardState = { ...i };
  }
  handleContentFileChange(i) {
    var s;
    const t = i.target, e = (s = t.files) == null ? void 0 : s[0];
    e && this.service.setContentFile(e, t);
  }
  handleMediaFileChange(i) {
    var s;
    const t = i.target, e = (s = t.files) == null ? void 0 : s[0];
    e && this.service.setMediaFile(e, t);
  }
  async handleContentImport() {
    await this.service.importContent();
  }
  async handleMediaImport() {
    await this.service.importMedia();
  }
  async handleContentExport() {
    const i = await this.service.exportContentResults();
    i && await _(i, "content-results.csv");
  }
  async handleMediaExport() {
    const i = await this.service.exportMediaResults();
    i && await _(i, "media-results.csv");
  }
  render() {
    const { activeTab: i, content: t, media: e } = this.dashboardState;
    return l`
      <uui-box>
        <div slot="header" class="dashboard-header">
          <h2>Bulk Upload</h2>
        </div>

        <!-- Tab Navigation -->
        <uui-tab-group>
          <uui-tab
            label="Content Import"
            ?active=${i === "content"}
            @click=${() => this.service.setActiveTab("content")}>
            Content Import
          </uui-tab>
          <uui-tab
            label="Media Import"
            ?active=${i === "media"}
            @click=${() => this.service.setActiveTab("media")}>
            Media Import
          </uui-tab>
        </uui-tab-group>

        <!-- Content Import Panel -->
        ${i === "content" ? l`
          <div class="tab-panel">
            ${this.renderInfoBox("content")}
            ${this.renderUploadSection("content", t)}
            ${t.loading ? this.renderLoadingState("content") : d}
            ${t.results && !t.loading ? this.renderResults("content", t.results) : d}
          </div>
        ` : d}

        <!-- Media Import Panel -->
        ${i === "media" ? l`
          <div class="tab-panel">
            ${this.renderInfoBox("media")}
            ${this.renderUploadSection("media", e)}
            ${e.loading ? this.renderLoadingState("media") : d}
            ${e.results && !e.loading ? this.renderResults("media", e.results) : d}
          </div>
        ` : d}
      </uui-box>
    `;
  }
  renderInfoBox(i) {
    return i === "content" ? l`
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
      ` : l`
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
  renderUploadSection(i, t) {
    const e = i === "content", s = e ? "content-file-input" : "media-file-input";
    return l`
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

          ${t.file && !t.loading ? l`
            <div class="file-info">
              <span class="file-icon">📄</span>
              <div class="file-details">
                <strong>${t.file.name}</strong>
                <span class="file-size">(${Q(t.file.size)})</span>
              </div>
            </div>
          ` : d}

          <div class="button-group">
            <uui-button
              label=${e ? "Import Content" : "Import Media"}
              look="primary"
              color="positive"
              ?disabled=${!t.file || t.loading}
              @click=${e ? this.handleContentImport : this.handleMediaImport}>
              ${t.loading ? "Processing..." : `▲ Import ${e ? "Content" : "Media"}`}
            </uui-button>
            ${t.file && !t.loading ? l`
              <uui-button
                label="Clear File"
                look="outline"
                @click=${() => e ? this.service.clearContentFile() : this.service.clearMediaFile()}>
                Clear
              </uui-button>
            ` : d}
          </div>
        </div>
      </uui-box>
    `;
  }
  renderLoadingState(i) {
    return l`
      <div class="loading-state">
        <uui-loader-bar></uui-loader-bar>
        <p>Importing ${i}, please wait...</p>
      </div>
    `;
  }
  renderResults(i, t) {
    const e = {
      total: t.totalCount || 0,
      success: t.successCount || 0,
      failed: t.failureCount || 0,
      successRate: t.totalCount > 0 ? Math.round(t.successCount / t.totalCount * 100) : 0
    };
    return l`
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
              @click=${i === "content" ? this.handleContentExport : this.handleMediaExport}>
              ⬇ Export Results
            </uui-button>
            <uui-button
              label="Clear Results"
              look="outline"
              color="default"
              @click=${() => i === "content" ? this.service.clearContentResults() : this.service.clearMediaResults()}>
              Clear Results
            </uui-button>
          </div>

          <!-- Results Table -->
          ${t.results && t.results.length > 0 ? l`
            <div class="results-table-container">
              <table class="results-table">
                <thead>
                  <tr>
                    <th>Status</th>
                    <th>Name</th>
                    ${i === "content" ? l`<th>Doc Type</th>` : l`<th>Media Type</th>`}
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  ${t.results.map((s) => l`
                    <tr class=${s.success ? "success" : "failed"}>
                      <td>${s.success ? "✅" : "❌"}</td>
                      <td>${s.name || "-"}</td>
                      <td>${i === "content" ? s.docTypeAlias : s.mediaTypeAlias || "-"}</td>
                      <td class="message-cell">${s.errorMessage || "Success"}</td>
                    </tr>
                  `)}
                </tbody>
              </table>
            </div>
          ` : d}
        </div>
      </uui-box>
    `;
  }
};
m.styles = U`
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
P([
  W()
], m.prototype, "dashboardState", 2);
m = P([
  I("bulk-upload-dashboard")
], m);
const et = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  get BulkUploadDashboardElement() {
    return m;
  }
}, Symbol.toStringTag, { value: "Module" })), at = (i, t) => {
  t.registerMany(T);
};
export {
  m as BulkUploadDashboardElement,
  at as onInit
};
//# sourceMappingURL=bulkupload.js.map
