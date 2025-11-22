# You are Codex — Generate Production-Ready ClickHouse Schema

You are Codex.  
Please generate a production-ready **ClickHouse database schema** based on the full specification below.

---

# 🎯 Goal

✔ A **main table** exists  
✔ When deleting a record in the **main table**,  
✔ All corresponding dependent records in **child tables** must ALSO be deleted  
✔ Implement cascade delete in ClickHouse using best practices  
   - (Because ClickHouse does not support real foreign keys)

**Use:**

- `MergeTree` or `ReplacingMergeTree`
- `UUID` for ids
- `Nullable` where needed
- `JSON` stored as `String`
- Use `TTL`, partitions, materialized views, or mutation cascades to simulate cascade deletes
- Output should be valid SQL using fenced code blocks:  
  \`\`\`sql  
  ...  
  \`\`\`

---

# 📌 MAIN TABLE — Root Entity: `main_entities`

This table serves as the *aggregate root*.  
All child tables reference its `entity_id`.

### Columns

- `entity_id` (UUID, PK)  
- `creator_id` (UInt64)  
- `creator_email` (String)  
- `title` (String)  
- `panels` (String) — JSON string  
- `collaborators` (String) — JSON string  
- `visibility` (String)  
- `is_shared` (UInt8)  
- `shared_token` (UUID)  
- `created_at` (DateTime64)  
- `updated_at` (DateTime64)  

### Cascade requirement
When deleting a row from `main_entities`,  
**all corresponding rows** from:  
- `tracking_sessions`  
- `tracking_events`  
- `dashboard_editors`  
- `dashboard_favorites`  

must also be removed.

Implement cascade delete using ClickHouse best practices.

---

# 📌 CHILD TABLE 1 — `tracking_sessions`

System-level session tracking.

### Columns

- `id` (UUID)  
- `entity_id` (UUID)  
- `user_id` (UInt64)  
- `company_id` (UInt64)  
- `started_at` (DateTime64)  
- `last_activity_at` (DateTime64)  
- `ended_at` (DateTime64)  
- `total_events` (UInt32)  
- `total_views` (UInt32)  
- `total_clicks` (UInt32)  
- `total_exposes` (UInt32)  
- `total_disappears` (UInt32)  
- `device_type` (String)  
- `device_model` (String)  
- `entry_page` (String)  
- `exit_page` (String)  
- `created_at` (DateTime64)

### Cascade
When the related `entity_id` is deleted in `main_entities`,  
rows in this table with the same `entity_id` must also be deleted.

---

# 📌 CHILD TABLE 2 — `tracking_events`

System-level front-end / app event tracking.

### Columns
- `id` (UUID)  
- `entity_id` (UUID)  
- `session_id` (UUID)  
- `event_type` (String)  
- `event_name` (String)  
- `page_name` (String)  
- `component_name` (String)  
- `timestamp` (DateTime64)  
- `refer` (String)  
- `expose_time` (Int32)  
- `user_id` (UInt64)  
- `company_id` (UInt64)  
- `device_type` (String)  
- `os_version` (String)  
- `browser_version` (String)  
- `network_type` (String)  
- `network_effective_type` (String)  
- `page_url` (String)  
- `page_title` (String)  
- `viewport_height` (Int32)  
- `properties` (String) — JSON string  

### Cascade requirement
Deleting `entity_id` from `main_entities` must also delete related event rows.

---

# 📌 CHILD TABLE 3 — `dashboard_editors`

### Columns
- `id` (UUID)  
- `entity_id` (UUID)  
- `user_email` (String)  
- `added_at` (DateTime64)  
- `added_by` (String)

### Cascade
Deleting `entity_id` must remove corresponding editor records.

---

# 📌 CHILD TABLE 4 — `dashboard_favorites`

### Columns
- `id` (UUID)  
- `entity_id` (UUID)  
- `user_id` (UInt64)  
- `created_at` (DateTime64)

### Cascade
Deleting `entity_id` must remove corresponding favorite records.

---

# 🎯 Notes for Codex

Please generate:

### ✔ `CREATE TABLE` statements  
### ✔ Using correct ClickHouse engines (`MergeTree`, `ReplacingMergeTree`, etc.)  
### ✔ With recommended partition keys and ordering keys  
### ✔ Implement cascade delete behavior using ClickHouse best practices:

Choose the best, modern, maintainable approach:

- Materialized View + DELETE
- TTL DELETE with partitioning by `entity_id`
- Cascading mutations
- Dictionary-based dependency removal
- Or any other recommended ClickHouse method

### Output must be valid SQL

Format:

\`\`\`sql
CREATE TABLE ...
\`\`\`

---

# END OF FILE —  
**Please generate the ClickHouse schema now.**
