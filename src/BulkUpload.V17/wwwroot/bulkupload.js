const a = {
  type: "section",
  alias: "BulkUpload.Section",
  name: "Bulk Upload",
  meta: {
    label: "Bulk Upload",
    pathname: "bulk-upload"
  }
}, o = {
  type: "dashboard",
  alias: "BulkUpload.Dashboard",
  name: "Bulk Upload Dashboard",
  element: () => import("./bulk-upload-dashboard.element-BBaLn02L.js"),
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
}, l = [a, o];
export {
  l as manifests
};
//# sourceMappingURL=bulkupload.js.map
