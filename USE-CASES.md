# BulkUpload for Umbraco — Use Case Analysis

## What the Tool Does (Core)
A native Umbraco backoffice package that bulk-imports and updates **content and media** via CSV/ZIP files, with 30+ property resolvers, hierarchy management, deduplication, and result tracking.

---

## Common CMS Client Problems

### USE CASES FOUND — Problems this tool already solves

#### Content Creation at Scale
| Problem | How BulkUpload Solves It |
|---|---|
| Product team needs to launch a 500-item catalogue but Umbraco only allows one item at a time | CSV with all product fields + ZIP with product images = single import |
| Client has a franchise with 200 location pages to create | `parent` column with auto-folder creation handles hierarchy automatically |
| Marketing need 50 event pages live before a campaign launches Monday | Bulk create with `bulkUploadShouldPublish=true` — all live in one upload |
| New staff directory needed with 80 team member pages and headshots | One CSV + one ZIP with all photos |
| Client exports their job listings from their HR system (Excel) and needs them in Umbraco weekly | Excel → Save as CSV → Upload. Repeatable every week |
| Restaurant group needs seasonal menus across 12 sites | One CSV per location or one CSV with parent GUIDs referencing each site |
| Legal firm needs 300 case study pages migrated in before a partner meeting | Structured CSV + media ZIP = done in minutes |

#### Content Updates at Scale
| Problem | How BulkUpload Solves It |
|---|---|
| SEO team needs to update meta descriptions on 400 product pages | Update-mode CSV with only the SEO columns populated — other fields untouched |
| Client rebrands — needs to swap hero images across 150 pages | URL resolver (`urlToMedia`) + update mode CSV — new images pulled from CDN |
| Pricing change across entire product catalogue | Export from their PIM, add `bulkUploadShouldUpdate=true` + GUIDs, re-import |
| Content team accidentally published wrong dates on 60 event pages | Correction CSV with `bulkUploadContentGuid` + correct date column |
| New compliance requirement means adding a disclaimer block to 200 pages | Update CSV targeting only the block list property |

#### CMS Migrations
| Problem | How BulkUpload Solves It |
|---|---|
| Client wants to move from WordPress / Craft / Kentico to Umbraco | Legacy ID mapping (`bulkUploadLegacyId` / `bulkUploadLegacyParentId`) preserves parent-child hierarchy |
| Old CMS IDs are referenced throughout the content — relationships will break | The legacy ID cache maps old IDs to new Umbraco UDIs automatically |
| Client has 10,000 pages across a deep content tree — order of creation matters | Topological sort across multi-CSV imports ensures parents always created before children |
| Content was spread across multiple export files from old system | Multi-CSV ZIP with automatic deduplication and cross-file relationship resolution |

#### Media Management
| Problem | How BulkUpload Solves It |
|---|---|
| 1,000 product images sitting in a shared drive with no structure in the media library | `pathToStream` resolver + parent path creates organised folder hierarchy |
| Client wants to pull approved images from their DAM/CDN into the media library | `urlToStream` resolver downloads directly from any HTTPS URL |
| Same product image referenced across multiple content pages — re-uploaded each time | Media deduplication detects duplicates across all CSV files in a batch |
| Images have no alt text — accessibility audit is failing | `altText` column in media CSV — populate for all items in one import |
| Media is buried in one flat folder — editors can't find anything | Parent path column creates organised sub-folder structure on import |

#### Repeatable / Operational Workflows
| Problem | How BulkUpload Solves It |
|---|---|
| Client runs weekly news imports from a wire service | Standard CSV template filled each week and uploaded |
| Recruitment site needs new job listings every Monday from ATS export | ATS export → format CSV → upload |
| Annual report site needs 5 years of archived documents added | One CSV with file paths or URLs, parent structure |
| Client team rotates press releases from a PR agency via spreadsheet | Agency fills in CSV template, client uploads — no Umbraco training needed for agency |
| eCommerce client gets product data from supplier as Excel — needs it in Umbraco | Excel → CSV → Upload. Resolver handles all field mapping |

#### Tracking & Auditing
| Problem | How BulkUpload Solves It |
|---|---|
| Client doesn't know which content was successfully created vs failed | Result CSV export with `bulkUploadSuccess` column and assigned GUIDs |
| Client needs the new Umbraco GUIDs to feed back into another system (e.g. CRM) | Result CSV contains all generated GUIDs for downstream use |
| Editor made a bulk import and now can't identify what was created | Result export is downloadable immediately after each run |

---

### USE CASES NOT FOUND — Problems the tool doesn't yet address

These are real client pain points where the tool could be extended or where a workaround would still be needed:

#### Content Governance & Scheduling
| Problem | Gap |
|---|---|
| Client needs content to go live at 3am on a specific date | No scheduled/future publish date column |
| Content needs to go through an approval workflow before publishing | No workflow integration — items publish immediately or not at all |
| Pages need to be unpublished / archived in bulk after a campaign | No bulk unpublish / archive mode |
| Client wants to preview bulk content before it goes live | No staging preview for imported content |

#### Language & Localisation
| Problem | Gap |
|---|---|
| Client has a multilingual site and needs to import content in 6 languages simultaneously | No language variant column — each language would need a separate import |
| Translated content arrives from a translation agency in a spreadsheet | Tool could support this with a `language` column — currently manual |

#### Content Quality & Validation
| Problem | Gap |
|---|---|
| CSV has missing required fields — import fails mid-way with no pre-validation | No pre-flight validation / dry-run mode before committing |
| Client uploads malformed data and doesn't know until after import runs | No schema validation against the document type before processing |
| Duplicate content already exists in Umbraco — import creates it again | No check-before-create option to skip existing items |

#### Automation & Integration
| Problem | Gap |
|---|---|
| Client wants imports to trigger automatically when a file lands in a folder | No scheduled or file-watch triggered imports |
| Client's ERP pushes product updates via webhook — they want Umbraco to sync | No API endpoint to trigger an import programmatically (files only) |
| Client uses Zapier / Make to automate content workflows | No webhook or integration point |
| Client wants real-time sync between their PIM and Umbraco | Tool is batch/point-in-time, not streaming |

#### Deletion & Cleanup
| Problem | Gap |
|---|---|
| Client ran an import and needs to roll it back | No undo / rollback using the result CSV |
| Outdated products need to be bulk-deleted or unpublished after a range date | No delete or bulk-unpublish mode |
| Orphaned media files from old imports are cluttering the library | No bulk-delete or cleanup tooling |

#### Reporting & Insight
| Problem | Gap |
|---|---|
| Manager wants a dashboard showing import history over time | No persistent import history or logs in the backoffice |
| Client wants to know how many items were updated vs created in a run | Result CSV shows success/fail but no summary statistics |
| Audit requirement to know who ran which import and when | No user attribution or timestamped audit log in the UI |

#### Rich Content & Complex Fields
| Problem | Gap |
|---|---|
| Client has rich text (RTE) fields with inline images and formatting | No HTML/Markdown-to-RTE resolver |
| Content has nested Block Grids 3 levels deep with complex structure | Block list/grid support exists but deeply nested structures may be complex to express in CSV |
| Client uses custom property editors from third-party packages | Would need a custom resolver written for each one |

---

## Summary

| Category | Covered | Not Covered |
|---|---|---|
| Bulk content creation | ✅ | |
| Bulk content updates | ✅ | |
| CMS migration (with hierarchy) | ✅ | |
| Media from ZIP / URL / path | ✅ | |
| Media deduplication | ✅ | |
| Result tracking / export | ✅ | |
| Scheduled/future publishing | | ❌ |
| Workflow integration | | ❌ |
| Multilingual / variant imports | | ❌ |
| Pre-flight validation / dry run | | ❌ |
| Bulk delete / unpublish | | ❌ |
| Automated / triggered imports | | ❌ |
| Import history dashboard | | ❌ |
| Rollback / undo | | ❌ |

The tool covers the **most painful and time-consuming** day-to-day problems — migration, catalogue launches, repeatable weekly workflows, and mass updates. The gaps are largely in automation, governance, and localisation — natural candidates for a product roadmap conversation with clients.
